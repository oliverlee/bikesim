#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""
Convert serial data in CSV format to XML and send via UDP.
"""
import argparse
import itertools
import math
import queue
import socket
import socketserver
import sys
import threading
import time

import serial
from lxml import etree

#import hanging_threads


DEFAULT_BAUDRATE = 115200
DEFAULT_ENCODING = 'utf-8'
DEFAULT_UDPHOST = 'localhost'
DEFAULT_UDPTXPORT = 9900
DEFAULT_UDPRXPORT = 9901

MAX_TORQUE_PEAK = 3.58
MAX_TORQUE_CONT = 2.12
TORQUE_SCALING_FACTOR = 1.0
TORQUE_LIMIT = MAX_TORQUE_PEAK
RAD_PER_DEG = 2*math.pi/360

ACT_QUEUE = queue.Queue(1)
SEN_QUEUE = queue.Queue(1)
WRITE_TIMEOUT = 0.05 # seconds
READ_TIMEOUT = 0.01 # seconds, timeout for reading most recent value
                    #          sensor/actuator queue in main thread
PRINT_LOOP_PERIOD = 0.1 # seconds, approx print loop time period


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
            if not math.isnan(torque):
                self.server.serial.write('{}\n'.format(torque).encode())
            try:
                ACT_QUEUE.get_nowait()
            except queue.Empty:
                pass
            ACT_QUEUE.put(['{}'.format(torque)])


class UdpServer(socketserver.UDPServer):
    def __init__(self, server_address, RequestHandlerClass,
                 serial_port, encoding):
        socketserver.UDPServer.__init__(self, server_address,
                                        RequestHandlerClass)
        self.serial = serial_port
        self.encoding = encoding


class Sample(object):
    _size = 4

    def __init__(self, delta=0, deltad=0, cadence=0, brake=0):
        self.delta = delta
        self.deltad = deltad
        self.cadence = cadence
        self.brake = brake

    @staticmethod
    def size():
        return Sample._size

    @classmethod
    def create_from_data(cls, data, delim=','):
        vals = [v.strip() for v in data.split(delim)]
        if len(vals) != cls._size:
            raise ValueError(vals, "Invalid input for {}()".format(__class__))
        s = Sample(float(vals[0]), float(vals[1]),
                   float(vals[2]), bool(vals[3]))
        return s

    def print(self, delim=','):
        return delim.join(str(val) for val in
                [self.delta, self.deltad, self.cadence, self.brake])

    def __str__(self):
        return self.print()

    def ff_list(self, float_format=':= 8.4f'):
        l1 = ['{{{}}}'.format(float_format).format(v)
              for v in [self.delta/RAD_PER_DEG, self.deltad/RAD_PER_DEG,
                        self.cadence]]
        l2 = [format(int(self.brake))]
        return l1 + l2

    def print_xml(self, enc=DEFAULT_ENCODING):
        root = etree.Element('root')
        etree.SubElement(root, "delta").text = str(self.delta)
        etree.SubElement(root, "deltad").text = str(self.deltad)
        etree.SubElement(root, "cadence").text = str(self.cadence)
        etree.SubElement(root, "brake").text = str(self.brake)
        return etree.tostring(root, encoding=enc)


class Receiver(object):
    def __init__(self, serial_port, data_delim=',', enc=DEFAULT_ENCODING):
        self.pieces = '' # incomplete sample
        self.q = queue.Queue() # queue of complete samples
        self.ser = serial_port
        self.delim = data_delim
        self.enc = enc

    def receive(self):
        """Receives any data available to be read on the serial port and
        divides it into samples. Returns True when a sample is available and
        False otherwise.
        """
        num_bytes = self.ser.inWaiting()
        if num_bytes > 0:
            self.pieces += self.ser.read(num_bytes).decode(self.enc)
            samples = self.pieces.split('\n')
            if len(samples) > 1:
                for s in samples[:-1]:
                    try:
                        sample = Sample.create_from_data(s)
                    except ValueError:
                        continue # invalid input
                    self.q.put(sample)
                self.pieces = samples[-1]
        return not self.q.empty()


def sensor_thread_func(ser, enc, addr, udp):
    utc_time_str = lambda: time.strftime('%Y-%m-%d %H:%M:%S', time.gmtime())
    utc_file_str = lambda: time.strftime('%y%m%d_%H%M%S', time.gmtime())
    receiver = Receiver(ser, enc=enc)

    with open('sensor_data_{}'.format(utc_file_str()), 'w') as log:
        log.write('sensor data log started at {} UTC\n'.format(utc_time_str()))
        while ser.isOpen():
            try:
                if not receiver.receive():
                    time.sleep(0) # no data ready, yield thread
                    continue
            except OSError:
                # serial port closed
                log.flush()
                break

            sample = receiver.q.get()
            udp.sendto(sample.print_xml(), addr)
            log.write(sample.print() + '\n')

            # provide most recent version to main thread queue
            try:
                SEN_QUEUE.get_nowait() # empty the queue
            except queue.Empty:
                pass
            SEN_QUEUE.put(sample)
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

    ser = serial.Serial(args.port, args.baudrate, writeTimeout=WRITE_TIMEOUT)
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

    def print_states(start_time):
       t = time.time() - t0
       try:
           act = ACT_QUEUE.get(timeout=READ_TIMEOUT)
           # change printing of act
           act = ['{:.6f}'.format(float(f)) for f in act]
       except queue.Empty:
           act = ['  -  ']
       try:
           sen = SEN_QUEUE.get(timeout=READ_TIMEOUT).ff_list()
       except queue.Empty:
           sen = itertools.repeat(' - ', Sample.size())

       print('\t'.join(itertools.chain(['{:8.4f}'.format(t)], act, sen)))

    t0 = time.time()
    try:
        while True:
            time.sleep(PRINT_LOOP_PERIOD)
            print_states(t0)
    except KeyboardInterrupt:
        print('Shutting down...')
    finally:
       server.shutdown() # stop UdpServer, actuator command transmission
       ser.write('0\n'.encode()) # send 0 value actuator torque
       ser.close() # close serial port, terminating sensor thread
       sensor_thread.join() # wait for sensor thread to terminate
       sys.exit(0)
