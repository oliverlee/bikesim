/* refer to: https://arduino.stackexchange.com/questions/203/sending-large-amounts-of-serial-data */

#ifndef STREAMSEND_H
#define STREAMSEND_H
#include "Arduino.h"


#define PACKET_NOT_FOUND 0
#define BAD_PACKET 1
#define GOOD_PACKET 2

// Set the Max size of the Serial Buffer or the amount of data you want to send+2
// You need to add 2 to allow the prefix and suffix character space to send.
#define MAX_SIZE 64


class StreamSend {
private:
static int getWrapperSize() { return sizeof(char)*2; }
static byte receiveObject(Stream &ostream, void* ptr, unsigned int objSize, unsigned int loopSize);
static byte receiveObject(Stream &ostream, void* ptr, unsigned int objSize, unsigned int loopSize, char prefixChar, char suffixChar);
static char _prefixChar; // Default value is s
static char _suffixChar; // Default value is e
static int _maxLoopsToWait;

public:
static void sendObject(Stream &ostream, void* ptr, unsigned int objSize);
static void sendObject(Stream &ostream, void* ptr, unsigned int objSize, char prefixChar, char suffixChar);
static byte receiveObject(Stream &ostream, void* ptr, unsigned int objSize);
static byte receiveObject(Stream &ostream, void* ptr, unsigned int objSize, char prefixChar, char suffixChar);
static boolean isPacketNotFound(const byte packetStatus);
static boolean isPacketCorrupt(const byte packetStatus);
static boolean isPacketGood(const byte packetStatus);

static void setPrefixChar(const char value) { _prefixChar = value; }
static void setSuffixChar(const char value) { _suffixChar = value; }
static void setMaxLoopsToWait(const int value) { _maxLoopsToWait = value; }
static const char getPrefixChar() { return _prefixChar; }
static const char getSuffixChar() { return _suffixChar; }
static const int getMaxLoopsToWait() { return _maxLoopsToWait; }

};

#endif // STREAMSEND_H



