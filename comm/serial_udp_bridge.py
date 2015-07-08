#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""
Convert serial data in CSV format to XML and send via UDP.
"""
import argparse
import marshal
import math
import queue
import socket
import socketserver
import struct
import sys
import threading
import time

import serial

#import hanging_threads


DEFAULT_BAUDRATE = 2000000 # 115200
DEFAULT_UDPHOST = 'localhost'
DEFAULT_UDPTXPORT = 9900
DEFAULT_UDPRXPORT = 9901

MAX_TORQUE_PEAK = 3.58
MAX_TORQUE_CONT = 2.12
TORQUE_SCALING_FACTOR = 1.0
TORQUE_LIMIT = MAX_TORQUE_PEAK
RAD_PER_DEG = 2*math.pi/360
WHEEL_RADIUS = 0.3 # m
DEFAULT_WHEEL_RATE = -1*4/WHEEL_RADIUS # m/s -> rad/s

g_log_queue = queue.Queue() # elements are (timestamp, 'log line')
SERIAL_WRITE_TIMEOUT = 0.005 # seconds
SERIAL_READ_TIMEOUT = 0.001 # seconds, timeout for reading most recent value
                           #          sensor/actuator queue in main thread
PRINT_LOOP_PERIOD = 0.1 # seconds, approx print loop time period

# TODO: Read these values from Arduino sources
SERIAL_START_CHAR = b's'
SERIAL_END_CHAR = b'e'
SERIAL_PAYLOAD_SIZE = 8 # 2 * sizeof(float)

DEFAULT_FLOAT_FORMAT = ':= 8.4f'
MARSHAL_VERSION = 4


#def info(type, value, tb):
#    if hasattr(sys, 'ps1') or not sys.stderr.isatty():
#        sys.__excepthook__(type, value, tb)
#    else:
#        import traceback, pdb
#        traceback.print_exception(type, value, tb)
#        print
#        pdb.pm()


def encode_torque(torque):
    return struct.pack('=cfc', SERIAL_START_CHAR, torque, SERIAL_END_CHAR)

def decode_state(data):
    if len(data) != 10: # 2*sizeof(float) + 2*sizeof(char)
        return None
    prefix_char, torque, lean, suffix_char = struct.unpack('=cffc', data)
    if prefix_char == SERIAL_START_CHAR and suffix_char == SERIAL_END_CHAR:
        return torque, lean
    return None


class UdpHandler(socketserver.BaseRequestHandler):
    def handle(self):
        data = self.request[0].strip()
        state = decode_state(data)
        if state is None:
            print('Invalid state received: ({}) {}'.format(len(data), data))
            return

        torque, lean = state
        self.server.torque = torque * TORQUE_SCALING_FACTOR
        if not math.isnan(self.server.torque):
            if abs(self.server.torque) > TORQUE_LIMIT: # saturate torque
                self.server.torque = math.copysign(TORQUE_LIMIT,
                                                   self.server.torque)
            serial_write(self.server.serial, encode_torque(self.server.torque))
        d = marshal.dumps((time.time() - self.server.start_time,
                           'actuator', [torque, lean]),
                          MARSHAL_VERSION)
        g_log_queue.put(d)


class UdpServer(socketserver.UDPServer):
    def __init__(self, server_address, RequestHandlerClass,
                 serial_port, start_time ):
        socketserver.UDPServer.__init__(self, server_address,
                                        RequestHandlerClass)
        self.serial = serial_port
        self.start_time = start_time
        self.torque = None


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
                [self.delta, self.deltad])
                #[self.delta, self.deltad, self.cadence, int(self.brake)])

    def to_list(self):
        return [self.delta, self.deltad, self.cadence, self.brake]

    def __str__(self):
        return self.print()

    def ff_list(self, float_format=DEFAULT_FLOAT_FORMAT):
        l1 = ['{{{}}}'.format(float_format).format(v)
              for v in [self.delta/RAD_PER_DEG, self.deltad/RAD_PER_DEG,
                        self.cadence]]
        l2 = [format(int(self.brake))]
        return l1 + l2

    def to_list(self):
        return [self.delta, self.deltad, self.cadence, self.brake]


class Receiver(object):
    def __init__(self, serial_port):
        self.byte_q = [] # queue of bytes/incomplete samples
        self.sample_q = queue.Queue() # queue of complete samples
        self.ser = serial_port

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
                        print('Invalid sample received: {}'.format(ex))
                else:
                    self.byte_q.append(b)
        return not self.sample_q.empty()


class SensorListener(threading.Thread):
    def __init__(self, serial_port, udp, addr, start_time):
        threading.Thread.__init__(self, name='sensor thread')
        self.ser = serial_port
        self.udp = udp
        self.addr = addr
        self.sample = None
        self.start_time = start_time

    def run(self):
        receiver = Receiver(ser)
        while self.ser.isOpen():
            try:
                if not receiver.receive():
                    time.sleep(0) # no data ready, yield thread
                    continue
            except OSError: # serial port closed
                break
            self.sample = receiver.sample_q.get()
            self.udp.sendto(struct.pack('=cfffc',
                SERIAL_START_CHAR, self.sample.delta,
                self.sample.deltad,
                DEFAULT_WHEEL_RATE,
                SERIAL_END_CHAR), self.addr)
            d = marshal.dumps((time.time() - self.start_time,
                               'sensor', self.sample.to_list()),
                              MARSHAL_VERSION)
            g_log_queue.put(d)


def utc_filename():
    return time.strftime('%y%m%d_%H%M%S_UTC', time.gmtime())


class Logger(threading.Thread):
    def __init__(self, subject, feedback_enabled):
        threading.Thread.__init__(self, name='log thread')
        self._terminate = threading.Event()
        self.subject = subject
        self.feedback_enabled = feedback_enabled

    def run(self):
        t0 = time.time()
        timestamp = t0
        filename = 'log_{}_{}_{}'.format(utc_filename(), self.subject,
                                         self.feedback_enabled)
        print('Logging sensor/actuator data to {}'.format(filename))
        with open(filename, 'wb') as log:
            marshal.dump(int(time.time()), log, MARSHAL_VERSION)
            while not g_log_queue.empty():
                try:
                    d  = g_log_queue.get_nowait()
                except queue.Empty:
                    # if nothing to write
                    time.sleep(0) # yield thread
                    continue
                log.write(d)
            marshal.dump(int(time.time()), log, MARSHAL_VERSION)
        print('Data logged to {}'.format(filename))

    def terminate(self):
        """Request Logger object to stop."""
        self._terminate.set()


def serial_write(ser, msg):
    """Windows will throw a SerialException with the message:
    WindowsError(0, 'The operation completed successfully')
    """
    try:
       ser.write(msg)
    except serial.SerialException as e:
        if 'The operation completed successfully' not in e.args[0]:
            raise


if __name__ == "__main__":
    #sys.excepthook = info
    parser = argparse.ArgumentParser(description=
        'Convert serial data in CSV format to XML and send via UDP and '
        'vice versa.')
    parser.add_argument('port',
        help='serial port for communication with arduino')
    parser.add_argument('subject',
        help='note numerical code for test subject in log filename')
    parser.add_argument('feedback',
        help='note if torque feedback is enabled in log filename')
    parser.add_argument('-b', '--baudrate',
        help='serial port baudrate ({})'.format(DEFAULT_BAUDRATE),
        default=DEFAULT_BAUDRATE, type=int)
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

    t0 = time.time()
    actuator = UdpServer(udp_rx_addr, UdpHandler, ser, t0)
    actuator_thread = threading.Thread(target=actuator.serve_forever)
    actuator_thread.daemon = True

    sensor = SensorListener(ser, udp_tx, udp_tx_addr, t0)

    log = Logger(args.subject, args.feedback)

    sensor.start()
    actuator_thread.start()

    print('{} using serial port {} at {} baud'.format(
        __file__, args.port, args.baudrate))
    print('transmitting UDP data on port {}'.format(args.udp_txport))
    print('receiving UDP data on port {}'.format(args.udp_rxport))

    def print_states(start_time):
        t = time.time() - t0
        if actuator.torque is not None:
            act = ['{{{}}}'.format(DEFAULT_FLOAT_FORMAT).format(actuator.torque)]
        else:
            act = [' - ']

        if sensor.sample is not None:
            sen = sensor.sample.ff_list()
        else:
            sen = Sample.size() * [' - ']
        print('\t'.join(['{{{}}}'.format(DEFAULT_FLOAT_FORMAT).format(t)] +
                        act + sen))

    try:
        while True:
            time.sleep(PRINT_LOOP_PERIOD)
            print_states(t0)
    except KeyboardInterrupt:
        print('Shutting down...')
    finally:
       actuator.shutdown() # stop UdpServer, actuator command transmission
       serial_write(ser, encode_torque(0)) # send 0 value actuator torque
       ser.close() # close serial port, terminating sensor thread
       log.start() # log all data
       log.terminate() # request logging thread terminate

       # wait for other threads to terminate
       log.join() # wait for logging to complete
       sensor.join() # wait for sensor thread to terminate
       actuator_thread.join() # wait for actuator thread to terminate

       sys.exit(0)
