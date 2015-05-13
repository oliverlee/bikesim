#!/usr/bin/env python3
# -*- coding: utf-8 -*-
import sys
import socketserver

from lxml import etree

class UdpHandler(socketserver.BaseRequestHandler):
    def handle(self):
        data = self.request[0].strip()
        print("{} wrote:".format(self.client_address[0]))
        print(data)
        self.server.decode_xml(data)

class UdpServer(socketserver.UDPServer):
    def __init__(self, server_address, RequestHandlerClass):
        socketserver.UDPServer.__init__(self, server_address,
                                        RequestHandlerClass)
        self.a = 1

    def decode_xml(self, xml):
        root = etree.fromstring(xml)
        print(root.find('delta').text)



if __name__ == "__main__":
    if len(sys.argv) > 1:
        port = int(sys.argv[1])
    else:
        port = 9900
    host = "localhost"
    print("udp server listening on port {}".format(port))
    server = UdpServer((host, port), UdpHandler)
    server.serve_forever()
