#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""
Convert serial data in CSV format to XML and send via UDP.
"""
import argparse
import socket

import serial
from lxml import etree


DEFAULT_BAUDRATE = 115200
DEFAULT_ENCODING = 'utf-8'
DEFAULT_UDPHOST = 'localhost'
DEFAULT_UDPPORT = 9900


class Sample(object):
    def __init__(self, delta=0, deltad=0, cadence=0, brake=0):
        self.delta = delta
        self.deltad = deltad
        self.cadence = cadence
        self.brake = brake
    
    def gen_xml(self, enc=DEFAULT_ENCODING):
        root = etree.Element('root')
        etree.SubElement(root, "delta").text = str(self.delta)
        etree.SubElement(root, "deltad").text = str(self.deltad)
        etree.SubElement(root, "cadence").text = str(self.cadence)
        etree.SubElement(root, "brake").text = str(self.brake)
        return etree.tostring(root, encoding=enc)
        

def parse_csv(data):
    vals = data.strip().split(',')
    s = Sample()
    s.delta = float(vals[0])
    s.deltad = float(vals[1])
    s.cadence = int(vals[2])
    s.brake = bool(vals[3])
    return s


if __name__ == "__main__":
    parser = argparse.ArgumentParser(description=
            'Convert serial data in CSV format to XML and send via UDP.')
    parser.add_argument('port', help='serial port for input data')
    parser.add_argument('-b', '--baudrate', help='serial port baudrate',
                        default=DEFAULT_BAUDRATE, type=int)
    parser.add_argument('-e', '--encoding', help='serial data encoding type',
                        default=DEFAULT_ENCODING)
    parser.add_argument('-H', '--udp_host', help='udp server host ip',
                        default=DEFAULT_UDPHOST)
    parser.add_argument('-P', '--udp_port', help='udp server port',
                        default=DEFAULT_UDPPORT, type=int)
    args = parser.parse_args()
    
    ser = serial.Serial(args.port, args.baudrate)
    udp_addr = (args.udp_host, args.udp_port)
    udp = socket.socket(socket.AF_INET, socket.SOCK_DGRAM)
    while True:
        s = parse_csv(ser.readline().decode(args.encoding))
        udp.sendto(s.gen_xml(), udp_addr)
