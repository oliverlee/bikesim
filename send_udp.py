#!/usr/bin/env python
# -*- coding: utf-8 -*-

import os
import sys
import socket

usage = "{0} <port> <string>".format(__file__)

if len(sys.argv) < 3:
    print(usage)
    sys.exit(1)

port = int(sys.argv[1])
message = bytearray(sys.argv[2] + "\n", 'utf-8')

sock = socket.socket(socket.AF_INET, socket.SOCK_DGRAM)
sock.sendto(message, ("localhost", port))

