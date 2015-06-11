#!/user/bin/env python3
# -*- coding: utf-8 -*-
"""
Send simualted sensor data to a serial port.
A virtual serial port can be created using socat with:
    $ socat -d -d pty,raw,echo=0 pty,raw,echo=0
"""
import argparse
import serial
import sys
import time


DEFAULT_BAUDRATE = 115200
DEFAULT_ENCODING = 'utf-8'

if __name__ == "__main__":
    parser = argparse.ArgumentParser(description=
        'Send simualted sensor data to a serial port.')
    parser.add_argument('port',
        help='serial port for communication with arduino')
    parser.add_argument('-b', '--baudrate',
        help='serial port baudrate ({})'.format(DEFAULT_BAUDRATE),
        default=DEFAULT_BAUDRATE, type=int)
    parser.add_argument('-e', '--encoding',
        help='serial data encoding type ({})'.format(DEFAULT_ENCODING),
        default=DEFAULT_ENCODING)
    args = parser.parse_args()

    ser = serial.Serial(args.port, args.baudrate)
    try:
        while True:
            time.sleep(0.001) # period ~ 0.001 s -> 1000 Hz
            ser.write('0, 0, 0, 0\n'.encode())
    except KeyboardInterrupt:
        pass
    ser.close() # close serial port
    sys.exit(0)
