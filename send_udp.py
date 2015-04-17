#!/usr/bin/env python
# -*- coding: utf-8 -*-

import os
import sys
import socket
from lxml import etree


#usage = "{0} <port> <message>".format(__file__)
usage = "{0} <port> <delta>".format(__file__)

if len(sys.argv) < 3:
    print(usage)
    sys.exit(1)

port = int(sys.argv[1])
#message = bytearray(sys.argv[2] + "\n", 'utf-8')
delta = float(sys.argv[2])

root = etree.Element("root")
etree.SubElement(root, 'delta').text = str(delta)
etree.SubElement(root, 'cadence').text = str(30.0)
message = etree.tostring(root, encoding='utf-8')

sock = socket.socket(socket.AF_INET, socket.SOCK_DGRAM)
sock.sendto(message, ("localhost", port))

