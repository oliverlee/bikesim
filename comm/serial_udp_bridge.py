#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""
Convert serial data in CSV format to XML and send via UDP.
"""
import argparse
import math
import queue
import signal
import socket
import socketserver
import sys
import threading
import time

import serial
from lxml import etree


DEFAULT_BAUDRATE = 115200
DEFAULT_ENCODING = 'utf-8'
DEFAULT_UDPHOST = 'localhost'
DEFAULT_UDPTXPORT = 9900
DEFAULT_UDPRXPORT = 9901


TORQUE_SCALING_FACTOR = 1.0
TORQUE_LIMIT = 30
RAD_PER_DEG = 2*math.pi/360


ACTQ = queue.Queue(1)
SENQ = queue.Queue(1)


def info(type, value, tb):
    if hasattr(sys, 'ps1') or not sys.stderr.isatty():
        sys.__excepthook__(type, value, tb)
    else:
        import traceback, pdb
        traceback.print_exception(type, value, tb)
        print
        pdb.pm()


class UdpHandler(socketserver.BaseRequestHandler):
    def handle(self):
        data = self.request[0].strip()
        root = etree.fromstring(data)
        elem = root.find('torque')
        if elem is not None:
            tau0 = elem.text
            # rescale and limit torque
            torque = float(tau0) * TORQUE_SCALING_FACTOR
            if abs(torque) > TORQUE_LIMIT:
                torque = math.copysign(TORQUE_LIMIT, torque)
            self.server.serial.write('{}\n'.format(torque).encode())
            try:
                ACTQ.get_nowait()
            except queue.Empty:
                pass
            ACTQ.put_nowait(['{}'.format(torque)])


class UdpServer(socketserver.UDPServer):
    def __init__(self, server_address, RequestHandlerClass,
                 serial_port, encoding):
        socketserver.UDPServer.__init__(self, server_address,
                                        RequestHandlerClass)
        self.serial = serial_port
        self.encoding = encoding


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
    if len(vals) != 4:
        return None
    s = Sample()
    s.delta = float(vals[0])
    s.deltad = float(vals[1])
    s.cadence = float(vals[2])
    s.brake = bool(vals[3])
    return s


def sensor_thread_func(ser, enc, addr, udp):
    utc_time_str = lambda: time.strftime('%Y-%m-%d %H:%M:%S', time.gmtime())
    utc_file_str = lambda: time.strftime('%y%m%d_%H%M%S', time.gmtime())
    with open('sensor_data_{}'.format(utc_file_str()), 'w') as log:
        log.write('sensor data log started at {} UTC\n'.format(utc_time_str()))
        while ser.isOpen():
            try:
                dat = ser.readline().decode(enc)
            except BlockingIOError:
                continue
            except TypeError:
                # TypeError thrown by serialposix when serial port is closed
                break
            s = parse_csv(dat)
            if s is None:
                continue
            log.write(dat.strip() + '\n')
            udp.sendto(s.gen_xml(), addr)
            try:
                SENQ.get_nowait()
            except queue.Empty:
                pass
            #SENQ.put_nowait(dat.strip().split(','))
            float_fmt = '{:= .4f}'
            datum = [
                float_fmt.format(s.delta / RAD_PER_DEG),
                float_fmt.format(s.deltad / RAD_PER_DEG),
                float_fmt.format(s.cadence),
                '{}'.format(s.brake)
            ]
            SENQ.put_nowait(datum)
        log.write('sensor data log terminated at {} UTC\n'.format(
            utc_time_str()))


if __name__ == "__main__":
    sys.excepthook = info
    parser = argparse.ArgumentParser(description=
        'Convert serial data in CSV format to XML and send via UDP and '
        'vice versa.')
    parser.add_argument('port',
        help='serial port for communication with arduino')
    parser.add_argument('-b', '--baudrate',
        help='serial port baudrate ({})'.format(DEFAULT_BAUDRATE),
        default=DEFAULT_BAUDRATE, type=int)
    parser.add_argument('-e', '--encoding',
        help='serial data encoding type ({})'.format(DEFAULT_ENCODING),
        default=DEFAULT_ENCODING)
    parser.add_argument('-H', '--udp_host',
        help='udp remote host ip ({})'.format(DEFAULT_UDPHOST),
        default=DEFAULT_UDPHOST)
    parser.add_argument('-P', '--udp_txport',
        help='udp tx port ({})'.format(DEFAULT_UDPTXPORT),
        default=DEFAULT_UDPTXPORT, type=int)
    parser.add_argument('-p', '--udp_rxport',
        help='udp rx port ({})'.format(DEFAULT_UDPRXPORT),
        default=DEFAULT_UDPRXPORT, type=int)
    args = parser.parse_args()

    ser = serial.Serial(args.port, args.baudrate)
    udp_tx_addr = (args.udp_host, args.udp_txport)
    udp_rx_addr = (args.udp_host, args.udp_rxport)
    udp_tx = socket.socket(socket.AF_INET, socket.SOCK_DGRAM)

    sensor_thread = threading.Thread(target=sensor_thread_func,
            args=(ser, args.encoding, udp_tx_addr, udp_tx))
    sensor_thread.daemon = True

    server = UdpServer(udp_rx_addr, UdpHandler, ser, args.encoding)
    actuator_thread = threading.Thread(target=server.serve_forever)
    actuator_thread.daemon = True

    sensor_thread.start()
    actuator_thread.start()
    print('{} using serial port {} at {} baud'.format(
        __file__, args.port, args.baudrate))
    print('transmitting UDP data on port {}'.format(args.udp_txport))
    print('receiving UDP data on port {}'.format(args.udp_rxport))

    t0 = time.time()
    qto = 0.01
    try:
        while True:
            time.sleep(0.1)
            t = time.time() - t0
            try:
                act = ACTQ.get(timeout=qto)
                # change printing of act
                act = ['{:.6f}'.format(float(f)) for f in act]
            except queue.Empty:
                act = ['  -  ']

            try:
                sen = SENQ.get(timeout=qto)
            except queue.Empty:
                sen = []

            print('\t'.join(['{:8.4f}'.format(t)] + act + sen))

    except KeyboardInterrupt:
       server.shutdown() # stop UdpServer and actuator command transmission
       ser.write('0\n'.encode()) # send 0 value actuator torque
       ser.close() # close serial port, terminating sensor thread
       sensor_thread.join() # wait for sensor thread to terminate
       sys.exit(0)
