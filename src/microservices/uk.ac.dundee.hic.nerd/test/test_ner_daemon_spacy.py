#!/usr/bin/env python3
# Test the SpaCy version of NERd by repeatedly sending it a known string
# and checking for a known response.  Can also check the Java version of
# course, using -p 1881, but the response might be different.
# Usage:  [-p port] [-l number_of_loops]
# Note that there's two nested loops, one for socket creation and one for
# repeated queries using that socket.

import argparse
import socket

prog_name = 'test_ner_daemon_spacy.py'
host = 'localhost'
port = 1882
loops = 1

parser = argparse.ArgumentParser(description = prog_name)
parser.add_argument('-p', '--port', dest='port', action="store", help=f'port number, default {port}')
parser.add_argument('-l', '--loop', dest='loop', action="store", help=f'number of times to loop, default {loops}')
args = parser.parse_args()
if args.port: port = int(args.port)
if args.loop: loops = int(args.loop)

for ii in range(0,loops):
    #print('Attempt %d' % ii)
    clientsocket = socket.socket(socket.AF_INET, socket.SOCK_STREAM)
    clientsocket.connect((host, port))
    for jj in range(0,loops):
        clientsocket.send(b'We are taking John to Queen Margaret Hospital today.\0')
        response = clientsocket.recv(1024)
        assert response == b'PERSON\x0014\x00John\x00PERSON\x0022\x00Queen Margaret Hospital\x00DATE\x0046\x00today\x00\x00'
    clientsocket.close()
