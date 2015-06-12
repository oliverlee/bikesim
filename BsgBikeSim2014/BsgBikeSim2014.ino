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
#include "butterlowpass.h"

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

    // Define the sampling frequency and sample time of the timer3
    const int FREQ = 50;        //frequency in [hz]. max 100!

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


    /*    Variables initialization for Serial communication*/
    // IEEE double has at most 17 significant decimal digits precision
    // TODO: don't send characters but instead send bits
    char inputString[30];
    char* currentInput = inputString;

    //bicycle state variables
    float delta = 0.0f;
    float deltaDot = 0.0f;
    ButterLowpass deltaDotFiltered;
    float v = 0.0f;
    volatile float cadence = 0.0f;

    boolean FeedbackMode = true;
    volatile int brakeState = LOW;
    volatile boolean sendFlag = false;

    /*    Declare objects */
    Adafruit_MCP4725 dac;    // The Digital to Analog converter attached via i2c
} // namespace

/* Utility functions */
String printFloat(float var){    //print a floating point number
    char dtostrfbuffer[15];
    return String(dtostrf(var, 6, 4, dtostrfbuffer));
}

float valToDelta(int val){// transform measured value to radians
    float myDelta = DEGTORAD*(SLOPE_DELTA * val + C_DELTA);
    return myDelta;
}

float valToDeltaDot(int val){// transform measured deltadot value to radians per second
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

float strToFloat(String s) {
    return atof(s.c_str());
}

void refreshSensorReads () {// Refresh the sensor reads of the Delta and Deltadot
    // Read the analog inputs.
    analogRead(DELTAPIN);                        // Read the value twice for stabalising
    delta = valToDelta(analogRead(DELTAPIN));    // * delta_toRad;
    analogRead(DELTADOTPIN);                    // Read twice for stabalising
    deltaDot = deltaDotFiltered.filter(valToDeltaDot(analogRead(DELTADOTPIN)));
}

void checkSerial() { //check and parse the serial incoming stream
    while (Serial.available()) {
        char c = (char)Serial.read();
        // set torque after endline
        if (c == '\n') {
            *currentInput = '\0';
            writeHandleBarTorque(atof(inputString));
            currentInput = inputString;
        } else {
            *currentInput++ = c;
        }
    }
}

// Start the simulation (timers)
void startSampling () {
    // Read the sensors:
    refreshSensorReads();    // Refresh the delta and deltadot with current values.
    startTimer();
}

// Stop the simulation
void stopSampling () {
    stopTimer();
}

void startTimer () {
    noInterrupts();
    TCNT3    =    0;
    TCCR3B    |=    ((1 << CS31)|(1 << WGM32));        // 8 prescaler(CS31) and ctc (WGM32) mode
    TIFR3    |=    (1 << OCF3A);                    // Clear the output compare flag by writing logic 1
    interrupts();
}
void stopTimer () {
    noInterrupts();
    TCCR3B = 0;
    TCNT3 = 0;
    interrupts();
}

void startCadenceCapture () {
    noInterrupts();
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
    TIMSK1 = (1 << ICIE1) | (1 << TOIE1);

    interrupts();
}

void writeHandleBarTorque (float t) {
    int val = constrain(torqueToDigitalOut(t), 0, 4095);
    dac.setVoltage(val, false); // set the torque. Flag when DAC not connected.
}

void sendState () {
    Serial.println(printFloat(delta) + "," + printFloat(deltaDot) + "," +
            printFloat(cadence) + "," + brakeState);
}

void brakeSignalchangeISR () {
    // Brake signal ISR handler which is called on pin change.
    //Get the brake level by reading the pin:
    brakeState = !digitalRead(BRAKEINPUTPIN);
}


ISR(TIMER1_CAPT_vect) {
//    // using timer 1 configured prescaler
//    const uint16_t t1_freq = 16000000/1024; // clk/sec
//    float c = 60.0f * t1_freq / ICR1; // rev/min
//
//    // if calculated cadence is reasonable, set value and reset counter
//    if (c < 200.0f) {
//        cadence = c;
//        TCNT1 = 0;
//    }
}

ISR(TIMER1_OVF_vect) {
    cadence = 0.0f;
}
/*--------------------- SETUP ()-------------------------------------------------*/
void setup()
{
    /*    Setup Timer3 for periodically calculating the simulator.
        Each timer interrupt corresponds to one time step solved.
    */

    // Pedal sensor pin
    pinMode(CADENCEPIN, INPUT_PULLUP); // should be input capture pin timer 1

    // Initialise the brake interrupt 0 on pin 2:
    pinMode(BRAKEINPUTPIN, INPUT_PULLUP);
    brakeSignalchangeISR();

    // Attach the interrupts:
    noInterrupts(); //disable all interrupts for the time being
    attachInterrupt(BRAKEHANDLE_INT, brakeSignalchangeISR, CHANGE);    // External interrupt of the brake pin

    // Setup the timer that raises a send state update flag with the prescribed frequency
    TCCR3A    = 0;
    TCCR3B    = 0;
    TCNT3    = 0;
    OCR3A    = 16000000/(8*FREQ)-1;    // Set the compare value. 16mhz(clock frequency)/8(prescaler)/frequency
    TIMSK3    = 0;
    TIMSK3    |= (1 << OCIE3A); //enable timer compare interrupts channel A on timer 1.

    interrupts();

    // pin modes:
    pinMode(MCENABLEPIN, OUTPUT);
    digitalWrite(MCENABLEPIN, FeedbackMode);

    // For Adafruit MCP4725A1 the address is 0x62 (default) or 0x63 (ADDR pin tied to VCC)
    dac.begin(0x62);
    // Set the initial value of 2.5 volt. Flag when done. (0 Nm)
    dac.setVoltage(2048, true);

    /*    Setup serial communication for external control and debugging purposes */
    Serial.begin(115200); //115200
    while (!Serial) {};                //Wait for the Serial to connect and do nothing. Needed for Leonardo only.

    // start the timed sending of state
    startTimer();
    // Start the cadence capture
    startCadenceCapture();
}

ISR(TIMER3_COMPA_vect) {    //Timer3 compare match interrupt service routine
    /*
        This is the timer interrupt service routine that is run with the sampling frequency.

        The analog inputs are read every cycle of the routine.
        The simulator only when the run flag is enabled.
    */
    refreshSensorReads();    // Refresh the delta and deltadot with current values.
    sendFlag = true;
}

void loop()
{
    /*    The main loop contains all communication and commands handling stuff.
        Basically the non time critical elements of the program.
        It starts with checking for serial messages received
    */
    if (sendFlag) {
        // Send the state over Serial communication if the sendFlag has been raised by the interrupt service routine of the timer
        sendState();    // Send the state of the hardware via Serial communication
        sendFlag = false;    // reset the send flag.
    }
    //Check here if incoming serial commands are available and process them accordingly
    checkSerial();
}
