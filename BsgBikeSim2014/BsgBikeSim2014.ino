/*
	title:	BsgBikeSim2014
	date:	18-12-2014
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

		TODO: AttachInterrupt on cadence sensor pin: (maybe not possible since we need to detect 0 speed as well. -> timeout)
		- measure the time the interrupt is fired
		- calculate the time since the last update
		- calculate cadence in RPM
		- update the cadence value variable

		In the Loop() routine:
		- if flag was raised:
			- set the noInterrupt flag (? should we?)
			- update the analog input signals from the steer angle and steer rate sensors
			- update the digital input of the brake signal
			- Send the sensory data (steer angle, steering rate, cadence, brake) over serial
				communication as a CSV in fixed order
		- if serial.available
			- add it to the serial input buffer until a full line is received
			- If full line received:
				- calculate the output voltage from the desired torque
				- Set the output voltage on the connected DAC module

*/

/*	Dependencies / Libraries */
#include <Wire.h>
#include <Adafruit_MCP4725.h>
#include <avr/io.h>
#include <avr/interrupt.h>

/*	Constants definitions */
// Pin assignments:
#define DELTAPIN A0
#define DELTADOTPIN A1
#define MCENABLEPIN 6	// Digital output pin enabling the motorcontroller
#define CADENCEPIN 4	// Input trigger for the cadence counter timer interrupt on Timer1
int BRAKEHANDLE_INT = 4;	// Input trigger for the brake signal. INT 4 is on pin 7 of Leonardo
#define BRAKEINPUTPIN 7	// Input trigger pin for the brake signal. attach it to external interrupt. Pin 7 for INT 4.

// Define conversion factor from measured analog signal to delta
#define DELTAMAXLEFT -32.0
#define DELTAMAXRIGHT 30.0
#define VALMAXLEFT 289.0
#define VALMAXRIGHT 680.0
#define SLOPE_DELTA (DELTAMAXRIGHT-DELTAMAXLEFT)/(VALMAXRIGHT-VALMAXLEFT)
#define C_DELTA DELTAMAXLEFT-SLOPE_DELTA*VALMAXLEFT
const float rr	= 0.3;		// [m] wheel radius rear

// Delta dot:
#define SLOPE_DELTADOT -0.24438
#define C_DELTADOT 125

// conversion factor from degrees to radians
#define DEGTORAD 3.14/180

// Define the sampling frequency and sample time of the timer3
#define FREQ 50		//frequency in [hz]. max 100!
#define CYCLETIME 1.0/FREQ		//sample time in [s]

// Define SERIAL parse commands
#define SETOUTPUTTORQUE 4
#define QUITOUTPUTTORQUE 3
#define APPLYOUTPUTTORQUE 2
#define RUN 1
#define QUIT 0

/*	Variables initialization for Serial communication*/
String inputString = "";         // a string to hold incoming data
boolean stringComplete = false;  // whether the string is complete

//bicycle state variables
float delta				= 0.0;
float deltaDot			= 0.0;
float v					= 0.0;
volatile unsigned int cadence			= 0;
volatile boolean cadenceOverflowFlag	= false;

float Td				= 0.0;
boolean run				= true;
boolean FeedbackMode	= true;
volatile int brakeState = LOW;
volatile boolean sendFlag = false;

/*	Declare objects */
Adafruit_MCP4725 dac;	// The Digital to Analog converter attached via i2c

/* Utility functions */
String printFloat(float var){	//print a floating point number
	char dtostrfbuffer[15];
	return String(dtostrf(var, 4,2, dtostrfbuffer));
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
	//int val = int(409.5 * torque + 2048); // Lijkt veel te sterk
	int val = int(-400 * torque + 2048); // Lijkt veel te sterk
	return val;
} 

String getNext(String& message){
	/*	Extract the next part of the csv input serial string. */
	int firstIndex = message.indexOf(',');	// Index of First occurence of the ','
	int len = message.length();			//Length of the curent inputString
	if (firstIndex == -1){
		// If no more commas:
		firstIndex = len;
	}	
	String next = message.substring(0,firstIndex);
	message = message.substring(firstIndex + 1, len);
	message.trim();
	next.trim();
	return next;
}

float strToFloat(String strVal) {
	/*	Convert the given string value to a floating point value
		2 decimals max.	*/
	char _strValChars[strVal.length()+1];
	strVal.toCharArray(_strValChars, strVal.length()+1);
	float myFloat = atof(_strValChars);
	return myFloat;
}

void refreshSensorReads () {// Refresh the sensor reads of the Delta and Deltadot
	// Read the analog inputs.
	analogRead(DELTAPIN);						// Read the value twice for stabalising
	delta = valToDelta(analogRead(DELTAPIN));	// * delta_toRad;
	analogRead(DELTADOTPIN);					// Read twice for stabalising
	deltaDot = valToDeltaDot(analogRead(DELTADOTPIN));	// * deltaDot_toRadSec;
}

void checkSerial() {//check and parse the serial incoming stream
	while (Serial.available()) {
		// get the new byte:
		char inChar = (char)Serial.read(); 		
		// if the incoming character is a newline, set a flag so the next loop can do something about it:
		if (inChar == '\n') {
		  stringComplete = true;
		  break;
		}
		// add the read character to the inputString:
		inputString += inChar;
	}
		
	/*	What to do when a complete line has been received:		*/
	if (stringComplete) {
		/* First post process string that is received. (data type?)
		Protocol is: CMD, ARGS*
		All comma seperated and spaces can be removed. ARGS* will be kept as a remainder of the total string that is received.
		*/
		String CMD = getNext(inputString);		// Get the requested ID from the message. First element.
		String stateString;
		boolean val;
		String flag;
		switch (CMD.toInt()) {
		case QUIT:
			// Stop the sampling
			stopSampling();
			break;

		case RUN:
			// Start sampling
			startSampling();
			break;

		case APPLYOUTPUTTORQUE:			
			// Apply the steer output torque send by the game
			digitalWrite(MCENABLEPIN, true);
			break;

		case QUITOUTPUTTORQUE:
			// do not apply the steer torque anymore even if it is send by the game
			digitalWrite(MCENABLEPIN, false);
			//sendState();
			break;
		case SETOUTPUTTORQUE:
			// Set the correct output torque value
			String torqueAsString = getNext(inputString);
			Td = strToFloat(torqueAsString);
			writeHandleBarTorque(Td);
		}
	// clear the string for the next round.
	inputString = "";
	stringComplete = false;
	}
}

// Start the simulation (timers)
void startSampling () {
	// Read the sensors:
	refreshSensorReads();	// Refresh the delta and deltadot with current values.	
	startTimer();
}

// Stop the simulation
void stopSampling () {
	stopTimer();
}

void startTimer () {
	noInterrupts();
	TCNT3	=	0;
	TCCR3B	|=	((1 << CS31)|(1 << WGM32));		// 8 prescaler(CS31) and ctc (WGM32) mode
	TIFR3	|=	(1 << OCF3A);					// Clear the output compare flag by writing logic 1
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
	TCNT1 = 0;
	TCCR1B	|=	((1 << WGM12)|(1 << CS10)|(1 << CS12)|(1 << ICNC1)|(1 << ICES1));	// Start the timer in the CTC mode. 1024 prescaler, input capture noise canceler and triggers on rising edge.
	TIFR1 |= ((1 << ICF1)|(1 << TOV1)|(1 << OCF1A)); //Clear the flag of the input capture and the overflow flag
	interrupts();
}
void stopCadenceCapture () {
	noInterrupts();
	TCCR1B = 0;
	TCNT1 = 0;
	interrupts();
}
void writeHandleBarTorque (float T) {
	int val = constrain(torqueToDigitalOut(T),0, 4095);
	dac.setVoltage(val, false);	// set the torque. Flag when DAC not connected.
}
void sendState () {
	// First make a temp variable of the cadence and brake because it could be changed while sending it.
	noInterrupts();
	unsigned int cadenceCopy = cadence;
	boolean brakeStateCopy = brakeState;
	// Maybe at the delta as well.
	interrupts();
	// Send the serial data
	Serial.println(printFloat(delta)+","+ printFloat(deltaDot)+","+ cadenceCopy +","+ brakeStateCopy);
}
void brakeSignalchangeISR () {
	// Brake signal ISR handler which is called on pin change.
	//Get the brake level by reading the pin:
	brakeState = !digitalRead(BRAKEINPUTPIN);
}
ISR(TIMER1_COMPA_vect) { // Timer3 compare match interrupt service routine
	/*
	This is the timer interrupt service routine that is run with the sampling frequency.
	The analog inputs are read every cycle of the routine.
	The simulator only when the run flag is enabled.
	*/
	cadence = 0;
	cadenceOverflowFlag = true;
}
ISR(TIMER1_CAPT_vect){
	if (cadenceOverflowFlag) {
		cadenceOverflowFlag = false;
		//cadence = 0;
	} else {
		unsigned long counter = ICR1;
		if (counter > 3125) { // = 300 rpm
			cadence = int(60*16000000/(1024*counter));
		} else {
			// If interrupt is too fast. Assume error
			//cadence = 0;
		}
	}
	// Reset the timer
	TCNT1 = 0;
	TIFR1 |= ((1 << ICF1)|(1 << TOV1)|(1 << OCF1A)); //Clear the flag of the input capture and the overflow flag
}
ISR(TIMER1_OVF_vect){
	// Not used
}
/*--------------------- SETUP ()-------------------------------------------------*/
void setup()
{
	/*	Setup Timer3 for periodically calculating the simulator.
		Each timer interrupt corresponds to one time step solved.
	*/
	
	// Pedal sensor pin
	pinMode(CADENCEPIN, INPUT_PULLUP); // should be input capture pin timer 1
	//digitalWrite(CADENCEPIN, HIGH);

	// Initialise the brake interrupt 0 on pin 2:
	pinMode(BRAKEINPUTPIN, INPUT_PULLUP);
	brakeSignalchangeISR();

	// Attach the interrupts:
	noInterrupts(); //disable all interrupts for the time being	
	attachInterrupt(BRAKEHANDLE_INT, brakeSignalchangeISR, CHANGE);	// External interrupt of the brake pin
	
	// Setup the timer that raises a send state update flag with the prescribed frequency
	TCCR3A	= 0;
	TCCR3B	= 0;
	TCNT3	= 0;
	OCR3A	= 16000000/(8*FREQ)-1;	// Set the compare value. 16mhz(clock frequency)/8(prescaler)/frequency. ‐1 omdat ctc 1 tick duurt
	TIMSK3	= 0;
	TIMSK3	|= (1 << OCIE3A); //enable timer compare interrupts channel A on timer 1.
	interrupts();
	
	// Now the input capture counter of the cadence on Timer1
	TCCR1A	=	0;
	TCCR1B	=	0;
	TCNT1	=	0;
	OCR1A	=	65530; //Set the compare value. Almost the maximum to prevent the overflow from occuring. Should be around 14 RPM.
	TIFR1	|=	((1 << ICF1)|(1 << TOV1)|(1 << OCF1A)); //Clear the flags of the input capture, overflow and output compare A.
	TIMSK1	|=	((1 << ICIE1)|(1 << TOIE1)|(1 << OCIE1A)); //enable: Input capture, output compare A and timer overflow.
	interrupts();
	
	// pin modes:
	pinMode(MCENABLEPIN, OUTPUT);
	digitalWrite(MCENABLEPIN, FeedbackMode);
	
	// For Adafruit MCP4725A1 the address is 0x62 (default) or 0x63 (ADDR pin tied to VCC)
	dac.begin(0x62);
	// Set the initial value of 2.5 volt. Flag when done. (0 Nm)
	dac.setVoltage(2048, true);

	/*	Setup serial communication for external control and debugging purposes */
	Serial.begin(115200); //115200
	while (!Serial) {};				//Wait for the Serial to connect and do nothing. Needed for Leonardo only. 
	// reserve 200 bytes for the inputString:
	inputString.reserve(200);

	// start the timed sending of state
	startTimer();
	// Start the cadence capture
	startCadenceCapture();
}

ISR(TIMER3_COMPA_vect) {	//Timer3 compare match interrupt service routine
	/*
		This is the timer interrupt service routine that is run with the sampling frequency.

		The analog inputs are read every cycle of the routine.
		The simulator only when the run flag is enabled.
	*/
	refreshSensorReads();	// Refresh the delta and deltadot with current values.
	sendFlag = true;
}

void loop()
{
	/*	The main loop contains all communication and commands handling stuff.
		Basically the non time critical elements of the program.
		It starts with checking for serial messages received
	*/
	if (sendFlag) {
		// Send the state over Serial communication if the sendFlag has been raised by the interrupt service routine of the timer
		sendState();	// Send the state of the hardware via Serial communication
		sendFlag = false;	// reset the send flag.
	}
	//Check here if incoming serial commands are available and process them accordingly
	checkSerial();
}
