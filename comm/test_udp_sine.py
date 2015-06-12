#!/usr/bin/env python3
# -*- coding: utf-8 -*-

import math
import time

import send_udp

port = 9901
t0 = time.time()

try:
    while True:
        time.sleep(0.01)
        t = time.time()
        dt = t - t0
        y = 2*math.sin(2*dt)
        send_udp.transmit_udp_xml(port, [str(y)]);
        print('{:.4f}: {}'.format(dt, y))
except KeyboardInterrupt:
    send_udp.transmit_udp_xml(port, ['0'])
