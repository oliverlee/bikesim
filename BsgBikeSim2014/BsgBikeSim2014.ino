/*
    title:    BsgBikeSim2014
    date:    18-12-2014
    author: Thom van Beek, thom@fietsenmakers.net +31648254856
    copyright Fietsenmakers Product developers 2014.

    description:
        This arduino software executes a bicycle simulator application on a hometrainer setup with a haptic steer.
        The arduino gets the sensory data. Does a first form of processing and sends it to the serial line on a regular
        basis/frequency. It senses:
        steer angle (delta) in [rad]
        steer rate (deltaDot) in [rad/s]
        pedal frequency (cadence) in [RPM]

        It also sends the steer output torque to the Maxon motor controller:
        Steer torque (Td) in [Nm]

        The way the arduino software implements this, in pseudocode, is:
        Interrupt Service Routine I:
        - set a timed interrupt on a regular interval.
        - in the Interrupt Service Routine (ISR) raise a flag
        - exit the ISR

        In the Loop() routine:
        - if flag was raised:
            - set the noInterrupt flag (? should we?)
            - update the analog input signals from the steer angle and steer
              rate sensors
            - update the digital input of the brake signal
            - Send the sensory data (steer angle, steering rate, cadence,
              brake) over serial communication as a CSV in fixed order
        - if serial.available
            - add it to the serial input buffer until a full line is received
            - If full line received:
                - calculate the output voltage from the desired torque
                - Set the output voltage on the connected DAC module

*/

/*    Dependencies / Libraries */
#include <Wire.h>
#include <Adafruit_MCP4725.h>
#include <avr/io.h>
#include <avr/interrupt.h>
#include "streamsend.h"
#include "sample.h"
#include "butterlowpass.h"

#define SERIAL_PREFIX_CHAR 's'
#define SERIAL_SUFFIX_CHAR 'e'

namespace {
    /*    Constants definitions */
    // Pin assignments:
    const int DELTAPIN = A0;
    const int DELTADOTPIN = A1;
    const int MCENABLEPIN = 6;  // Digital output pin enabling the motorcontroller
    const int CADENCEPIN = 4; // Input trigger for the cadence counter timer interrupt on Timer1
    const int BRAKEHANDLE_INT = 4; // Input trigger for the brake signal. INT 4 is on pin 7 of Leonardo
    const int BRAKEINPUTPIN = 7; // Input trigger pin for the brake signal.
                                 // attach it to external interrupt. Pin 7 for INT 4.

    // Define conversion factor from measured analog signal to delta
    const float DELTAMAXLEFT = -32.0f;
    const float DELTAMAXRIGHT = 30.0f;
    const float VALMAXLEFT = 289.0f;
    const float VALMAXRIGHT = 680.0f;
    const float SLOPE_DELTA = (DELTAMAXRIGHT - DELTAMAXLEFT)/
        (VALMAXRIGHT - VALMAXLEFT);
    const float C_DELTA = DELTAMAXLEFT - SLOPE_DELTA*VALMAXLEFT;

    // Delta dot:
    const float SLOPE_DELTADOT = -0.24438f;
    const float C_DELTADOT = 125.0f;

    // conversion factor from degrees to radians
    const float DEGTORAD = 3.14/180;

    // Define the sampling frequency serial tranmission frequency with TIMER3
    //   Use no more than 100 Hz for serial transmission rate where
    //   SERIAL_TX_FREQ = SAMPLING_FREQ/SERIAL_TX_PRE
    const int SAMPLING_FREQ = 50;
    const int SERIAL_TX_PRE = 1; // prescaler for serial transmission

    // Define constants for converstion from torque to motor PWM
    const float maxon_346970_max_current_peak = 3.0f; // A
    const float maxon_346970_max_current_cont = 1.780f; // A
    const float maxon_346970_torque_constant = 0.217f; // Nm/A
    const float maxon_346970_max_torque_peak =
        maxon_346970_max_current_peak * maxon_346970_torque_constant; // Nm
    const float maxon_346970_max_torque_cont =
        maxon_346970_max_current_cont * maxon_346970_torque_constant; // Nm

    // measured with calipers
    const float gearwheel_mechanical_advantage = 11.0f/2.0f;

    /*    Variable initialization for Serial communication*/
    const int rxBufferSize = 2*sizeof(float);
    char rxBuffer[rxBufferSize]; // buffer to receive actuation torque
    int rxBufferIndex = 0;

    // watchdog counter and limit to disable torque
    int torqueWatchDog = 0;
    const int TORQUE_WATCHDOG_LIMIT = 10;

    //bicycle state struct
    Sample sample;
    int sampleCount = 0;

    /*    Declare objects */
    Adafruit_MCP4725 dac;    // The Digital to Analog converter attached via i2c
} // namespace

/* Utility functions */
String printFloat(float var){ //print a floating point number
    char dtostrfbuffer[15];
    return String(dtostrf(var, 6, 4, dtostrfbuffer));
}

float valToDelta(int val){ // bits to rad
    float myDelta = DEGTORAD*(SLOPE_DELTA * val + C_DELTA);
    return myDelta;
}

float valToDeltaDot(int val){ // bits to rad/s
    float myDelta = DEGTORAD*(SLOPE_DELTADOT * val + C_DELTADOT);
    return myDelta;
}

int torqueToDigitalOut (float torque) {
    const int pwm_zero_offset = 2048;
    const int max_pwm_int = 2048;
    float act_torque = torque / gearwheel_mechanical_advantage;
    // pwm needs to be negated, likely due to wiring
    int pwm = static_cast<int>(
            -max_pwm_int*act_torque/maxon_346970_max_torque_peak);
    return pwm + pwm_zero_offset;
}

void readSensors() {
    // Read analog sensor values for delta, deltadot
    sample.delta = valToDelta(analogRead(DELTAPIN));
    sample.deltaDot = valToDeltaDot(analogRead(DELTADOTPIN));
    ++sampleCount;
}

void checkSerial() { // check and parse the serial data
    while (Serial.available()) {
        char c = Serial.read();
        if (rxBufferIndex >= rxBufferSize) {
            rxBufferIndex = 0;
        }

        if (c != SERIAL_SUFFIX_CHAR) {
            rxBuffer[rxBufferIndex++] = c;
            continue;
        }

        // if we have the suffix character
        if (rxBufferIndex < (sizeof(float) + 1)) {
            rxBuffer[rxBufferIndex++] = c;
            continue;
        }

        int startIndex = rxBufferIndex - sizeof(float) - 1;
        if (rxBuffer[startIndex] == SERIAL_PREFIX_CHAR) {
            float torque;
            memcpy(&torque, &rxBuffer[startIndex + 1], sizeof(float));
            writeHandleBarTorque(torque);
        }
    }
}


void configSampleTimer () {
    // NOTE: interrupts must be disabled when configuring timers
    TCNT3 = 0; // set counter to zero

    // Timer Control Register A/B
    //   Waveform Generation Mode - 0100: Clear Time on Compare match (CTC)
    //   Input Capture Edge Select - 1: rising edge trigger
    //   Clock Select - 010: clk/8 (from prescaler)
    TCCR3A = 0;
    TCCR3B = ((1 << CS31)|(1 << WGM32));

    // Interupt Mask Register - Interrupt Enable:
    //   Output Compare A Match
    TIMSK3 = (1 << OCIE3A); // enable timer compare interrupts channel A on timer 1.

    // Output Compare Register
    //   16 MHz/PRESCALER/SAMPLING_FREQ
    OCR3A = 16000000/(8*SAMPLING_FREQ) - 1;
}

void configCadenceTimer () {
    // NOTE: interrupts must be disabled when configuring timers
    TCNT1 = 0; // set counter to zero

    // Timer Control Register A/B
    //   Waveform Generation Mode - 0000: Normal
    //   Input Capture Noise Canceler
    //   Input Capture Edge Select - 1: rising edge trigger
    //   Clock Select - 101: clk/1024 (from prescaler)
    TCCR1A = 0;
    TCCR1B = ((1 << ICNC1) | (1 << ICES1) | (1 << CS12) | (1 << CS10));

    // Interupt Mask Register - Interrupt Enable:
    //   Input Capture, Overflow
    //TIMSK1 = (1 << ICIE1) | (1 << TOIE1);
}

void writeHandleBarTorque (float t) {
    int val = constrain(torqueToDigitalOut(t), 0, 4095);
    dac.setVoltage(val, false); // set the torque. Flag when DAC not connected.
    if (t != 0.0f) {
        torqueWatchDog = TORQUE_WATCHDOG_LIMIT;
    }
}

//void brakeSignalchangeISR () {
//    // Brake signal ISR handler which is called on pin change.
//    //Get the brake level by reading the pin:
//    ./brakeState = !digitalRead(BRAKEINPUTPIN);
//}


//FIXME cadence calculations
//ISR(TIMER1_CAPT_vect) {
//    // using timer 1 configured prescaler
//    const uint16_t t1_freq = 16000000/1024; // clk/sec
//    float c = 60.0f * t1_freq / ICR1; // rev/min
//
//    // if calculated cadence is reasonable, set value and reset counter
//    if (c < 200.0f) {
//        cadence = c;
//        TCNT1 = 0;
//    }
//}

//ISR(TIMER1_OVF_vect) {
//    cadence = 0.0f;
//}

ISR(TIMER3_COMPA_vect) {
    readSensors();
}

void setup() {
    // configure input/output pins
    pinMode(CADENCEPIN, INPUT_PULLUP); // should be input capture pin timer 1
    pinMode(BRAKEINPUTPIN, INPUT_PULLUP);
    pinMode(MCENABLEPIN, OUTPUT);

    // enable motor controller
    digitalWrite(MCENABLEPIN, HIGH);

    // Attach the interrupts and configure timers
    noInterrupts();
    //attachInterrupt(BRAKEHANDLE_INT, brakeSignalchangeISR, CHANGE);
    configSampleTimer(); // sample and serial transmission timer
    configCadenceTimer();
    interrupts();

    // For Adafruit MCP4725A1 the address is 0x62 (default) or 0x63 (ADDR pin tied to VCC)
    dac.begin(0x62);
    // Set the initial value of 2.5 volt. Flag when done. (0 Nm)
    dac.setVoltage(2048, true);

    Serial.begin(2000000);
    while (!Serial); // wait for Serial to connect. Needed for Leonardo only.
}

void loop() {
    if (sampleCount >= SERIAL_TX_PRE) {
        StreamSend::sendObject(Serial, &sample, sizeof(sample),
                SERIAL_PREFIX_CHAR, SERIAL_SUFFIX_CHAR);
        sampleCount = 0;
    }

    // Check if incoming serial commands are available and process them
    checkSerial();
    if ((torqueWatchDog > 0) && (--torqueWatchDog == 0)) {
        writeHandleBarTorque(0);
    }
}
