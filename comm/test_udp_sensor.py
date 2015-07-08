#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""
Send simulated sensor data via UDP, bypassing udp-serial bridge.
"""
import argparse
import math
import sys
import time

import send_udp

SERIAL_START_CHAR = b's'
SERIAL_END_CHAR = b'e'
TRANSMISSION_FREQ = 50 # Hz
UDP_PORT = 9900
WHEEL_RADIUS = 0.3 # m


if __name__ == "__main__":
    parser = argparse.ArgumentParser(description=
        'Send simualted sensor data to udp port.')
    parser.add_argument('port',
        help='udp port', type=int)
    args = parser.parse_args()

    t0 = time.time()
    thetar = -5/WHEEL_RADIUS # m/s -> rad/s
    try:
        while True:
            time.sleep(1/TRANSMISSION_FREQ)
            dt = time.time() - t0
            delta = 0.03*math.sin(20*dt)
            deltad = 10 * 0.1*math.cos(10*dt)
            #send_udp.transmit_sensors(args.port, [delta, deltad])
            send_udp.transmit_sensors(args.port, [delta, deltad, thetar])
    except KeyboardInterrupt:
        pass
    sys.exit(0)
