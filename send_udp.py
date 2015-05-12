#!/usr/bin/env python3
# -*- coding: utf-8 -*-

import os
import sys
import socket
from lxml import etree


#usage = "{0} <port> <message>".format(__file__)
usage = ("{0} <port> <delta> <deltad> <cadence>".format(__file__) +
         "\nor\n{0} <port> <torque>".format(__file__))

if len(sys.argv) != 3 and len(sys.argv) != 5:
    print(len(sys.argv))
    print(usage)
    sys.exit(1)

port = int(sys.argv[1])
#message = bytearray(sys.argv[2] + "\n", 'utf-8')

root = etree.Element("root")
if len(sys.argv) == 3:
    etree.SubElement(root, "torque").text = str(float(sys.argv[2]))
else:
    etree.SubElement(root, "delta").text = str(float(sys.argv[2]))
    etree.SubElement(root, "deltad").text = str(float(sys.argv[3]))
    etree.SubElement(root, "cadence").text = str(float(sys.argv[4]))
message = etree.tostring(root, encoding='utf-8')

sock = socket.socket(socket.AF_INET, socket.SOCK_DGRAM)
sock.sendto(message, ("localhost", port))

