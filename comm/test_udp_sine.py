#!/usr/bin/env python3
# -*- coding: utf-8 -*-

import math
import time

import send_udp

port = 9901
t0 = time.time()

while True:
    time.sleep(0.1)
    t = time.time()
    y = 2*math.sin(2*(t - t0))
    send_udp.transmit_udp_xml(port, [str(y)]);

    try:
        pass
    except KeyboardInterrupt:
        break

# TODO: Capture KeyboardInterrupt and send zero torque on exit
send_udp.transmit_udp_xml(port, ['0'])
