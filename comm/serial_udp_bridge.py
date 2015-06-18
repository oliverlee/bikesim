#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""
Convert serial data in CSV format to XML and send via UDP.
"""
import argparse
import math
import queue
import socket
import socketserver
import struct
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
LOG_QUEUE = queue.Queue() # elements are (timestamp, 'log line')
SERIAL_WRITE_TIMEOUT = 0.05 # seconds
SERIAL_READ_TIMEOUT = 0.01 # seconds, timeout for reading most recent value
                           #          sensor/actuator queue in main thread
PRINT_LOOP_PERIOD = 0.1 # seconds, approx print loop time period
LOG_FLUSH_PERIOD = 0.1 # seconds

# TODO: Read these values from Arduino sources
SERIAL_START_CHAR = b's'
SERIAL_END_CHAR = b'e'
SERIAL_PAYLOAD_SIZE = 8 # 2 * sizeof(float)

DEFAULT_FLOAT_FORMAT = ':= 8.4f'


def info(type, value, tb):
    if hasattr(sys, 'ps1') or not sys.stderr.isatty():
        sys.__excepthook__(type, value, tb)
    else:
        import traceback, pdb
        traceback.print_exception(type, value, tb)
        print
        pdb.pm()


def encode_torque(torque):
    return struct.pack('=cfc', SERIAL_START_CHAR, torque, SERIAL_END_CHAR)


class UdpHandler(socketserver.BaseRequestHandler):
    def handle(self):
        data = self.request[0].strip()
        root = etree.fromstring(data)
        elem = root.find('torque')
        if elem is not None:
            tau0 = elem.text

            # rescale and limit torque
            torque = float(tau0) * TORQUE_SCALING_FACTOR
            if not math.isnan(torque):
                if abs(torque) > TORQUE_LIMIT: # saturate torque
                    torque = math.copysign(TORQUE_LIMIT, torque)
                self.server.serial.write(encode_torque(torque))

            LOG_QUEUE.put((time.time(), tau0 + '\n'))

            # provide most recent torque to main thread queue
            try:
                ACT_QUEUE.get_nowait()
            except queue.Empty:
                pass
            ACT_QUEUE.put(torque)


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
    def decode(cls, data):
        # TODO: Read struct format from Arduino sources
        if data[0] != SERIAL_START_CHAR:
            msg = "Start character not detected in sample, len {}"
            raise ValueError(msg.format(len(data) - 1))
        try:
            delta, deltad = struct.unpack('=ff', struct.pack('=8c', *data[1:]))
        except struct.error:
            raise ValueError("Invalid struct size: {}".format(len(data) - 1))
        return Sample(delta, deltad, 0, 0)

    def print(self, delim=','):
        return delim.join(str(val) for val in
                [self.delta, self.deltad, self.cadence, int(self.brake)])

    def __str__(self):
        return self.print()

    def ff_list(self, float_format=DEFAULT_FLOAT_FORMAT):
        l1 = ['{{{}}}'.format(float_format).format(v)
              for v in [self.delta/RAD_PER_DEG, self.deltad/RAD_PER_DEG,
                        self.cadence]]
        l2 = [format(int(self.brake))]
        return l1 + l2

    def print_xml(self, enc=DEFAULT_ENCODING):
        root = etree.Element('root')
        etree.SubElement(root, "delta").text = str(self.delta)
        etree.SubElement(root, "deltad").text = str(self.deltad)
        etree.SubElement(root, "cadence").text = str(0)
        etree.SubElement(root, "brake").text = str(0)
        return etree.tostring(root, encoding=enc)


class Receiver(object):
    def __init__(self, serial_port, data_delim=',', enc=DEFAULT_ENCODING):
        self.byte_q = [] # queue of bytes/incomplete samples
        self.sample_q = queue.Queue() # queue of complete samples
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
            byte_data = struct.unpack('={}c'.format(num_bytes),
                                      self.ser.read(num_bytes))
            for b in byte_data:
                if b == SERIAL_END_CHAR:
                    if len(self.byte_q) < (SERIAL_PAYLOAD_SIZE + 1):
                        # this is part of the payload
                        self.byte_q.append(b)
                        continue
                    if len(self.byte_q) > (SERIAL_PAYLOAD_SIZE + 1):
                        # last end char wasn't received
                        sample_bytes = self.byte_q[-(SERIAL_PAYLOAD_SIZE + 1):]
                    else:
                        sample_bytes = self.byte_q
                    self.byte_q = []
                    try:
                        sample = Sample.decode(sample_bytes)
                        self.sample_q.put(sample)
                    except ValueError as ex: #invalid input
                        print('Invalid sample recevied: {}'.format(ex))
                else:
                    self.byte_q.append(b)
        return not self.sample_q.empty()


def sensor_thread_func(ser, enc, addr, udp):
    receiver = Receiver(ser, enc=enc)
    while ser.isOpen():
        try:
            if not receiver.receive():
                time.sleep(0) # no data ready, yield thread
                continue
        except OSError: # serial port closed
            break

        sample = receiver.sample_q.get()
        udp.sendto(sample.print_xml(), addr)
        LOG_QUEUE.put((time.time(), '{}\n'.format(sample)))

        # provide most recent sample to main thread queue
        try:
            SEN_QUEUE.get_nowait() # empty the queue
        except queue.Empty:
            pass
        SEN_QUEUE.put(sample)

def utc_time():
    return time.strftime('%Y-%m-%d %H:%M:%S', time.gmtime())

def utc_filename():
    return time.strftime('%y%m%d_%H%M%S_UTC', time.gmtime())


class Logger(threading.Thread):
    def __init__(self):
        threading.Thread.__init__(self, name='log thread')
        self._terminate = threading.Event()

    def run(self):
        t0 = time.time()
        last_flush = t0
        timestamp = t0
        filename = 'log_{}'.format(utc_filename())
        print('Logging sensor/actuator data to {}'.format(filename))
        with open(filename, 'w') as log:
            log.write('Simulator log started at {} UTC\n'.format(utc_time()))
            while not self._terminate.is_set():
                try:
                    timestamp, data = LOG_QUEUE.get_nowait()
                except queue.Empty:
                    # if nothing to write
                    if timestamp - last_flush > LOG_FLUSH_PERIOD:
                        last_flush = timestamp
                        log.flush() # manually flush so we can see data
                        time.sleep(LOG_FLUSH_PERIOD/4)
                    else:
                        time.sleep(0) # yield thread
                    continue
                log.write('{}: {}'.format(timestamp - t0, data))
                time.sleep(0) # yield thread
            log.write('Simulator log terminated at {} UTC\n'.format(
                utc_time()))
        print('Data logged to {}'.format(filename))

    def terminate(self):
        """Request Logger object to stop."""
        self._terminate.set()


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

    ser = serial.Serial(args.port, args.baudrate,
                        writeTimeout=SERIAL_WRITE_TIMEOUT)
    udp_tx_addr = (args.udp_host, args.udp_txport)
    udp_rx_addr = (args.udp_host, args.udp_rxport)
    udp_tx = socket.socket(socket.AF_INET, socket.SOCK_DGRAM)

    sensor_thread = threading.Thread(target=sensor_thread_func,
            args=(ser, args.encoding, udp_tx_addr, udp_tx))
    sensor_thread.daemon = True

    server = UdpServer(udp_rx_addr, UdpHandler, ser, args.encoding)
    actuator_thread = threading.Thread(target=server.serve_forever)
    actuator_thread.daemon = True

    log = Logger()

    sensor_thread.start()
    actuator_thread.start()
    log.start()

    print('{} using serial port {} at {} baud'.format(
        __file__, args.port, args.baudrate))
    print('transmitting UDP data on port {}'.format(args.udp_txport))
    print('receiving UDP data on port {}'.format(args.udp_rxport))

    def print_states(start_time):
       t = time.time() - t0
       try:
           act = ACT_QUEUE.get(timeout=SERIAL_READ_TIMEOUT)
           # change printing of act
           act = ['{{{}}}'.format(DEFAULT_FLOAT_FORMAT).format(f) for f in act]
       except queue.Empty:
           act = ['  -  ']
       try:
           sen = SEN_QUEUE.get(timeout=SERIAL_READ_TIMEOUT).ff_list()
       except queue.Empty:
           sen = Sample.size() * [' - ']

       print('\t'.join(['{{{}}}'.format(DEFAULT_FLOAT_FORMAT).format(t)] +
                        act + sen))

    t0 = time.time()
    try:
        while True:
            time.sleep(PRINT_LOOP_PERIOD)
            print_states(t0)
    except KeyboardInterrupt:
        print('Shutting down...')
    finally:
       log.terminate() # request logging thread terminate
       server.shutdown() # stop UdpServer, actuator command transmission
       ser.write(encode_torque(0)) # send 0 value actuator torque
       ser.close() # close serial port, terminating sensor thread

       # wait for other threads to terminate
       log.join() # wait for logging to complete
       sensor_thread.join() # wait for sensor thread to terminate
       actuator_thread.join() # wait for actuator thread to terminate

       sys.exit(0)
