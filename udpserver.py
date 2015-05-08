#!/usr/bin/env python
# -*- coding: utf-8 -*-
import socketserver

class UDPHandler(socketserver.BaseRequestHandler):
    def handle(self):
        data = self.request[0].strip()
        socket = self.request[1]
        print("{} wrote:".format(self.client_address[0]))
        print(data)
        socket.sendto(data.upper(), self.client_address)

if __name__ == "__main__":
    host, port = "localhost", 9900
    server = socketserver.UDPServer((host, port), UDPHandler)
    server.serve_forever()
