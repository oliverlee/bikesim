#!/usr/bin/env python3
# -*- coding: utf-8 -*-
import sys
import socketserver

class UDPHandler(socketserver.BaseRequestHandler):
    def handle(self):
        data = self.request[0].strip()
        socket = self.request[1]
        print("{} wrote:".format(self.client_address[0]))
        print(data)
        socket.sendto(data.upper(), self.client_address)

if __name__ == "__main__":
    if len(sys.argv) > 1:
        port = int(sys.argv[1])
    else:
        port = 9900
    host = "localhost"
    server = socketserver.UDPServer((host, port), UDPHandler)
    print("udp server listening on port {}".format(port))
    server.serve_forever()
