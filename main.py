# -*- coding: utf-8 -*-
"""
Created December 19th 2014

@author: Thom van Beek

"""
#Import needed libraries or classes:
from lxml import etree

from twisted.internet.protocol import ClientFactory
from twisted.protocols.basic import LineReceiver
from twisted.internet.serialport import SerialPort
from twisted.internet.protocol import Protocol, Factory
from twisted.internet import reactor
from twisted.internet.task import LoopingCall
from twisted.internet import stdio

import time
import optparse
import StringIO
import csv
import sys
import datetime

def parse_args():
    usage = """usage: %prog [options]
        This is the bicycle simulator middleware
        Run it like this: python main.py --iface <ip adres to serve> --port <port> --com <Arduino com port>

        """

    parser = optparse.OptionParser(usage)

    help = "The port to listen on. Default to a random available port."
    parser.add_option('--port', type='int', help=help, default=13000)

    help = "The interface to listen on. Default is localhost."
    parser.add_option('--iface', help=help, default='localhost')

    help = "The com port to connect to the arduino. Default is COM4."
    parser.add_option('--com', help=help, default='COM4')

    help = "The baud rate of the com port to connect to the arduino. Default is 115200."
    parser.add_option('--baud', help=help, default='115200')

    help = "The update frequency of the arduino. Default is 50."
    parser.add_option('--update', help=help, default='50')

    options, args = parser.parse_args()

    return options

class InputReceiver(LineReceiver):
    #from os import linesep as delimiter
    delimiter = '\n'
    def __init__ (self, myFactory) :
        self.factory = myFactory

    def connectionMade(self):
        print(">>> hit key to quit >>>")

    def lineReceived(self, line):
        self.factory.shutDown()

class ArduinoSerialClient(LineReceiver):

    def __init__(self, id, torqueMode, updateFrequency, factory):
        self.lastLine = {}

        self.logFlag = False
        self.tcpFactory = factory
        self.id = id
        self.torqueMode = torqueMode

        #calculate filename
        date = datetime.datetime.now()
        self.filename = "./data/"+str(self.id)+"_"+str(self.torqueMode)+"_"+"_"+date.strftime("%Y%m%d_%H_%M_%S")+".csv"
        self.headersWrite = ['timestamp','id','torqueMode','delta','v','cadence','brake']
        self.fw = open(self.filename, 'ab')
        self.writer = csv.DictWriter(self.fw, self.headersWrite)
        pass

    def connectionLost (self, reason) :
        self.fw.close()
        print 'File closed and arduino connection lost. Reason: ', reason

    def connectionFailed(self):
        print "Connection to arduino Failed:", self

    def connectionMade(self):
        print 'Connected to arduino..'
        time.sleep(0.2)   #Give the arduino time to reset

    def startSampling(self):
        print "Start taking samples"
        if (self.torqueMode > 0):
            tm = '1'
            self.sendLine('2')
        else:
            tm = '0'
            self.sendLine('3')

        self.sendLine('1')

    def lineReceived(self, line):
        """
        Just send it through. Other things later.
        """
        headers = ['delta','v','cadence','brake']
        fr = StringIO.StringIO(line)
        reader = csv.DictReader(fr, headers, delimiter=',')

        print line  # Comment for normal operation

        for row in reader:
            row['timestamp'] = datetime.datetime.utcnow()
            row['id'] = self.id
            row['torqueMode'] = self.torqueMode
        self.writer.writerow(row)
        if row['brake']:    # Check if brake was set. Otherwise do not send to game.
            self.lastLine = row
            self.sendStateUpdate()  # To game

    def shutDown (self):
        self.sendLine('0')

    def getStateUpdate(self):
        pass

    def sendStateUpdate(self):
        # Create XML string first: (use the last known serial input)
        root = etree.Element("root")
        #for key, value in self.lastLine.iteritems():
        #for key in keysToSend:
        try:
            etree.SubElement(root, 'delta').text = self.lastLine['delta']
            etree.SubElement(root, 'v').text = self.lastLine['v']
            etree.SubElement(root, 'cadence').text = self.lastLine['cadence']
            etree.SubElement(root, 'brake').text = self.lastLine['brake']
        except:
            pass

        # Send it over TCP to the game:
        self.tcpFactory.sendUpdate(etree.tostring(root, pretty_print=False))
        pass

class TcpXmlEcho(LineReceiver):

    def lineReceived(self, line):
        try:
            #print "Received reply..:"
            root = etree.XML(line)
            #print etree.tostring(root, pretty_print = True)

        except:
            print "The middleware was not able to reconstruct an XML form the received string. The following was received from the server: "+line
        # Send it to the arduino
        TdElement = root[0]
        self.factory.arduino.sendLine("4,"+TdElement.text)

    def connectionMade(self):
        print "TCP connection made. Starting serial data throughput."
        #pass
        self.factory.client_list.append(self)
        self.factory.arduino.startSampling()

    def connectionLost (self, reason):
        print 'The TCP connection was lost due to: ', reason
        self.factory.arduino.shutDown()

class TcpXmlEchoClientFactory(ClientFactory):

    protocol = TcpXmlEcho

    def __init__(self):
        self.client_list = []
        pass

    def shutDown(self):
        # disconnect the TCP
        for cli in self.client_list:
            cli.transport.loseConnection()

        # stop the arduino sim if not already stopped
        self.arduino.shutDown()
        self.arduino.transport.loseConnection()
        print 'Shut down succesfully'

    def setArduinoConnection(self, arduinoClient):
        self.arduino = arduinoClient

    def sendUpdate(self, line):
        for cli in self.client_list:
            cli.sendLine(line)
        print line

    def startedConnecting(self, connector):
        print 'Started to connect.'

    def clientConnectionLost(self, connector, reason):
        print 'Lost connection.  Reason:', reason
        #reactor.stop()

    def clientConnectionFailed(self, connector, reason):
        print 'TCP Connection with game failed. Reason:', reason

    def clientConnectionMade(self, connector, reason):
        pass


def bikeSimMiddleware_main():
    """
    This is the main function to run when this file is run as the main program.
    The program serves the bikeSim game with the data from the Arduino simulator hardware.
    It basically translates the serial communication to TCP communication.
    """

    # Get the options passed as arguments to the program.
    options = parse_args()
    riderID = 0
    torqueMode = 0
    #velocity = 0
    #riderID = raw_input('Please enter the rider ID number: (default = 0)') or riderID
    torqueMode = raw_input('Please enter 1 if torque feedback should be enabled or 0 to ride without torque feedback on the handlebars.(default = 0)') or torqueMode
    #velocity = raw_input('Please enter the velocity of the bicycle: (default = 5.0)') or velocity

    tcpFactory = TcpXmlEchoClientFactory()
    myArduino = ArduinoSerialClient(riderID, torqueMode, options.update, tcpFactory )
    tcpFactory.setArduinoConnection(myArduino)

    SerialPort(myArduino, options.com, reactor, options.baud)
    myInputReceiver = InputReceiver(tcpFactory)
    stdio.StandardIO(myInputReceiver)
    reactor.connectTCP(options.iface, options.port, tcpFactory)
    reactor.run()

if __name__ == '__main__':
    bikeSimMiddleware_main()
