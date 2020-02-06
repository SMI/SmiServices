#!/usr/bin/env python3
# Runs a NER daemon on the port and interface specified below.
# Reads nul-terminated string, responds with nul-separated and nul-terminated
# string having fields: classification, start char, named entity
# and a final nul when no more entities have been detected.
# pip install spacy and pip install the model package first.
# Note unlike the java version it returns phrases not just words.

spacy_model = 'en_core_web_sm'      # the language model for NER
text_encoding = 'utf-8'             # XXX assumed

import socket
import threading
import spacy
import sys

class ThreadedServer(object):
    def __init__(self, host, port):
        self.host = host
        self.port = port
        self.sock = socket.socket(socket.AF_INET, socket.SOCK_STREAM)
        self.sock.setsockopt(socket.SOL_SOCKET, socket.SO_REUSEADDR, 1)
        self.sock.bind((self.host, self.port))
        self.nlp = spacy.load(spacy_model)

    def run(self):
        self.sock.listen(5)
        while True:
            client, address = self.sock.accept()
            client.settimeout(60)
            threading.Thread(target = self.listenToClient, args = (client, address)).start()

    def listenToClient(self, client, address):
        size = 4096
        while True:
            try:
                data = client.recv(size)
                if data:
                    response = ''
                    # replace \0 with . then decode from utf-8 bytes to string then do NER
                    ner_doc = self.nlp(data.replace(b'\0', b'.').decode(text_encoding))
                    for entity in ner_doc.ents:
                            label = entity.label_
                            if label == 'ORG': label = 'ORGANIZATION'
                            # pack the response into nul-separated fields
                            response += ("%s\0%d\0%s\0" % (label, entity.start_char, entity.text))
                    if response == '': response += '\0'
                    response += '\0'
                    # convert from utf-8 back to bytes for transmission
                    client.send(response.encode(text_encoding))
                else:
                    return
            except Exception as e:
                client.close()
                return False


if __name__ == "__main__":
    host_name=''
    port_num = int(sys.argv[1]) if len(sys.argv)>1 else 1881
    ThreadedServer(host_name, port_num).run()
