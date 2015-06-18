#!/user/bin/env python3
# -*- coding: utf-8 -*-
"""
Send simualted sensor data to a serial port.
A virtual serial port can be created using socat with:
    $ socat -d -d pty,raw,echo=0 pty,raw,echo=0
"""
import argparse
import math
import serial
import struct
import sys
import time


DEFAULT_BAUDRATE = 115200
SERIAL_START_CHAR = b's'
SERIAL_END_CHAR = b'e'
SEND_RATE = 50 # Hz

if __name__ == "__main__":
    parser = argparse.ArgumentParser(description=
        'Send simualted sensor data to a serial port.')
    parser.add_argument('port',
        help='serial port for communication with arduino')
    parser.add_argument('-b', '--baudrate',
        help='serial port baudrate ({})'.format(DEFAULT_BAUDRATE),
        default=DEFAULT_BAUDRATE, type=int)
    args = parser.parse_args()

    ser = serial.Serial(args.port, args.baudrate)
    t0 = time.time()
    try:
        while True:
            time.sleep(1/SEND_RATE)
            dt = time.time() - t0
            delta = 2*math.sin(3*dt)
            deltad = 2*math.cos(4*dt)
            packet = struct.pack('=cffc', SERIAL_START_CHAR, delta,
                                 deltad, SERIAL_END_CHAR)
            ser.write(packet)
    except KeyboardInterrupt:
        pass
    ser.close() # close serial port
    sys.exit(0)
