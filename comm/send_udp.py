#!/usr/bin/env python3
# -*- coding: utf-8 -*-

import os
import sys
import socket
import struct

SERIAL_START_CHAR = b's'
SERIAL_END_CHAR = b'e'

def transmit_udp_xml(port, args):
    num_args = len(args)
    args = [float(a) for a in args]
    args.append(SERIAL_END_CHAR)
    packet = struct.pack('=c{}fc'.format(num_args),
                         SERIAL_START_CHAR, *args)
    print(len(packet), packet)
    sock = socket.socket(socket.AF_INET, socket.SOCK_DGRAM)
    sock.sendto(packet, ("localhost", port))


if __name__ == "__main__":
    usage = ("{0} <port> <delta> <deltad>".format(__file__) +
             "\nor\n{0} <port> <torque>".format(__file__))

    if len(sys.argv) != 3 and len(sys.argv) != 4:
        print(len(sys.argv))
        print(usage)
        sys.exit(1)

    transmit_udp_xml(int(sys.argv[1]), sys.argv[2:])
