/* refer to: https://arduino.stackexchange.com/questions/203/sending-large-amounts-of-serial-data */

#include "streamsend.h"

//Preset Some Default Variables
//Can be modified when seen fit
char StreamSend::_prefixChar = 's';   // Starting Character before sending any data across the Serial
char StreamSend::_suffixChar = 'e';   // Ending character after all the data is sent
int StreamSend::_maxLoopsToWait = -1; //Set to -1 for size of current Object and wrapper

/**
* sendObject
*
* Converts the Object to bytes and sends it to the stream
*
* @param Stream to send data to
* @param ptr to struct to fill
* @param size of struct
* @param character to send before the data stream (optional)
* @param character to send after the data stream (optional)
*/
void StreamSend::sendObject(Stream &ostream, void* ptr, unsigned int objSize) {
    sendObject(ostream, ptr, objSize, _prefixChar, _suffixChar);
}

void StreamSend::sendObject(Stream &ostream, void* ptr, unsigned int objSize,
        char prefixChar, char suffixChar) {
    if (MAX_SIZE >= objSize+getWrapperSize()) { //make sure the object isn't too large
        byte * b = (byte *) ptr; // Create a ptr array of the bytes to send
        ostream.write((byte)prefixChar); // Write the suffix character to signify the start of a stream

        // Loop through all the bytes being send and write them to the stream
        for (unsigned int i = 0; i<objSize; i++) {
            ostream.write(b[i]); // Write each byte to the stream
        }
        ostream.write((byte)suffixChar); // Write the prefix character to signify the end of a stream
    }
}

/**
* receiveObject
*
* Gets the data from the stream and stores to supplied object
*
* @param Stream to read data from
* @param ptr to struct to fill
* @param size of struct
* @param character to send before the data stream (optional)
* @param character to send after the data stream (optional)
*/
byte StreamSend::receiveObject(Stream &ostream, void* ptr,
        unsigned int objSize) {
    return receiveObject(ostream, ptr, objSize, _prefixChar, _suffixChar);
}
byte StreamSend::receiveObject(Stream &ostream, void* ptr, unsigned int
        objSize, char prefixChar, char suffixChar) {
    return receiveObject(ostream, ptr, objSize, 0, prefixChar, suffixChar);
}

byte StreamSend::receiveObject(Stream &ostream, void* ptr,
        unsigned int objSize, unsigned int loopSize, char prefixChar,
        char suffixChar) {
    int maxLoops = (_maxLoopsToWait == -1) ?
        (objSize+getWrapperSize()) : _maxLoopsToWait;
    if (loopSize >= maxLoops) {
        return PACKET_NOT_FOUND;
    }
    if (ostream.available() >= (objSize+getWrapperSize())) { // Packet meets minimum size requirement
        if (ostream.read() != (byte)prefixChar) {
            // Prefix character is not found
            // Loop through the code again reading the next char
            return receiveObject(ostream, ptr, objSize, loopSize+1, prefixChar,
                    suffixChar);
        }

        char data[objSize]; //Create a tmp char array of the data from Stream
        ostream.readBytes(data, objSize); //Read the # of bytes
        memcpy(ptr, data, objSize); //Copy the bytes into the struct

        if(ostream.read() != (byte)suffixChar) {
            //Suffix character is not found
            return BAD_PACKET;
        }
        return GOOD_PACKET;
    }
    return PACKET_NOT_FOUND; //Prefix character wasn't found so no packet detected
}


boolean StreamSend::isPacketNotFound(const byte packetStatus) {
    return (packetStatus == PACKET_NOT_FOUND);
}

boolean StreamSend::isPacketCorrupt(const byte packetStatus) {
    return (packetStatus == BAD_PACKET);
}

boolean StreamSend::isPacketGood(const byte packetStatus) {
    return (packetStatus == GOOD_PACKET);
}




