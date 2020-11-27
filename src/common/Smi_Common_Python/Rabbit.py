#!/usr/bin/env python3
#
# Python classes for using RabbitMQ:
#   class CTP_Start_Message(smiMessage)
#   class IsIdentifiable_Start_Message(smiMessage)
# and functions for creating those messages and sending them:
#   send_CTP_Start_Message(yaml_dict, input_file, extraction_dir, project_name)
#   get_CTP_Output_Message(yaml_dict, num_messages_to_read)
#   send_IsIdentifiable_Start_Message(yaml_dict, extraction_directory, anonymised_filename, project_number)
#
# 1.01 abrooks Tue 27 Oct 15:30:01 GMT 2020 - changed Queue name for CTP
#      and added new fields to CTP message.

# XXX bugs: cannot insert OriginalPublishTimestamp as a long.
# XXX currently only used for testing so contains debug statements.
# XXX needs specific classes for the messages and for the queues
#  so we can hold a message object and ack it later not when received.

import os   # for os.path.basename()
import yaml # for yaml.load()
import uuid # for uuid.uuid4()
import json # for json.dumps()
import time # for time.time() and time.gmtime() and time.strftime()
import pika # for rabbit API
import sys


#LOGGER = logging.getLogger(__name__)
#LOGGER.info('message')


# ----------------------------------------------------------------------
# These two internal functions create data structures specific to pika

def _get_pika_connection_parameters(yaml_dict):
    """ Returns RabbitMQ connection params in pika struct
        Parameter: yaml_dict is the whole of default.yaml as a python dict
        Returns: struct suitable for passing to pika.BlockingConnection"""
    return pika.ConnectionParameters(
        host=yaml_dict['RabbitOptions']['RabbitMqHostName'],
        port=yaml_dict['RabbitOptions']['RabbitMqHostPort'],
        virtual_host=yaml_dict['RabbitOptions']['RabbitMqVirtualHost'],
        credentials=pika.PlainCredentials(
            username=yaml_dict['RabbitOptions']['RabbitMqUserName'],
            password=yaml_dict['RabbitOptions']['RabbitMqPassword'])
    )

def _get_pika_message_properties(producerExecutableName, producerProcessId = -1):
    """ Returns populated message properties for publishing to RabbitMQ
        Parameters: this executable name and PID
        Returns: struct suitable for passing to the properties arg of pika_model.basic_publish """
    ts = round(time.time())
    if producerProcessId == -1:
        producerProcessId = os.getpid()
    return pika.BasicProperties(
        delivery_mode=2,
        timestamp=ts,
        content_encoding="UTF-8",
        content_type="application/json",
        headers={
            "MessageGuid": str(uuid.uuid4()),
            "ProducerExecutableName": str(producerExecutableName),
            "ProducerProcessID": int(producerProcessId),
            #"OriginalPublishTimestamp": int(ts), # XXX causes class java.lang.Integer cannot be cast to class java.lang.Long AND similar in .net because they expect long but pika sends int.
            "Parents": ""
        }
    )


# ----------------------------------------------------------------------

class RabbitConsumer:
    """ Holds the connection state
    """
    def __init__(self, yaml_dict):
        self._yaml_dict = yaml_dict

    def __repr__(self):
        return '<RabbitConsumer>'

    def open(self, queue_name):
        self._pika_connection = pika.BlockingConnection(_get_pika_connection_parameters(self._yaml_dict))
        self._pika_model = pika_connection.channel()
        self._queue_name = queue_name

    def close(self):
        self._pika_model.close()
        self._pika_connection.close()


    def getMessage(self):
        """ Get one message and then stop even if more messages exist
        Return a tuple of (id, dict)
        where id can be used to ack the message later
        FRAME = <Basic.Deliver(['consumer_tag=ctag1.f35af930808c4153a87730acd05ced1b', 'delivery_tag=1', 'exchange=TEST.ExtractedFileStatusExchange', 'redelivered=False', 'routing_key=verify'])>
        PROP = <BasicProperties(['content_encoding=UTF-8', 'content_type=application/json', 'delivery_mode=2', "headers={'MessageGuid': '131c4c9d-ffc9-405c-a415-e5d64e9fe66b', 'OriginalPublishTimestamp': 0L, 'ProducerProcessID': 1307850216, 'ProducerExecutableName': 'CTPAnonymiserHost', 'Parents': '2da15b24-d0a8-40e0-8bdb-355ffefbfb21'}", 'timestamp=1606407'])>
        BODY = [{'DicomFilePath': 'report10.dcm', 'OutputFilePath': 'report10.dcm', 'Status': 'Anonymised', 'StatusMessage': '', 'ExtractionJobIdentifier': '68b41346-2342-4e44-87bb-2e168a01c676', 'ProjectNumber': '001', 'ExtractionDirectory': '001', 'JobSubmittedAt': '2020-11-26T16:12Z', 'IsIdentifiableExtraction': False, 'IsNoFilterExtraction': False}]
        """

        # Receive one message
        #  method_frame contains delivery_tag, needed for ack
        #                        exchange, and routing_key not needed
        # properties contains headers, probably not needed
        # body is (in our case) a JSON string
        method_frame, properties, body in pika_model.consume(self._queue_name)

        # Convert message body from JSON string into Python dict
        msg_dict = json.loads(body)

        # Cancel the consumer and return any pending messages (Important!)
        requeued_messages = pika_model.cancel()
        #print('Requeued %i messages' % requeued_messages)

        return (method_frame.delivery_tag, msg_dict)

    def ackMessage(self, delivery_tag):
        # Acknowledge the message
        self._pika_model.basic_ack(delivery_tag)



class RabbitProducer:
    """ Holds the connection state
    """
    def __init__(self, yaml_dict, name = "myProducerName", exchange = "", routingKey = ""):
        """ If you leave exchange="" then routingKey is used as the actual queue name
        """
        self._yaml_dict = yaml_dict
        self._producerName = name
        self._producerPID = os.getpid()
        self._exchange = exchange
        self._routingKey = routingKey

    def __repr__(self):
        return '<RabbitProducer>'

    def open(self):
        self._pika_connection = pika.BlockingConnection(_get_pika_connection_parameters(self._yaml_dict))
        self._pika_model = self._pika_connection.channel()

    def close(self):
        self._pika_model.close()
        self._pika_connection.close()

    def sendMessage(self, msg : smiMessage):
        pika_model.basic_publish(exchange = self._exchange,
                        routing_key = self._routingKey,
                        body = msg.to_json(),
                        properties = _get_pika_message_properties(self._producerName, self._producerPID),
                        mandatory = True)


# ----------------------------------------------------------------------
class smiMessage:
    """ SMI message base class.
    Currently only handles sending/output messages.
    Would need a from_json to handle receiving/input messages.
    Inherited by microservice-specific messages. """
    def __init__(self):
        self.msg_dict = {}

    def to_json(self):
        return json.dumps(self.msg_dict, default=lambda o: o.__dict__, sort_keys=True, indent=4)

    def __repr__(self):
        return '<smiMessage>: '+self.to_json()


class CTP_Start_Message(smiMessage):
    """ SMI message to send a file to CTP.
    The yaml_dict is default.yaml but only needed for debugging. """

    def __init__(self, yaml_dict, dicom_file_path, extraction_directory, project_number):
        super().__init__()
        self.msg_dict = {}
        self.msg_dict['DicomFilePath'] = dicom_file_path
        self.msg_dict['ExtractionDirectory'] = extraction_directory
        self.msg_dict['OutputPath'] = os.path.basename(dicom_file_path) #"op" #yaml_dict['FileSystemOptions']['ExtractRoot']
        self.msg_dict['ExtractionJobIdentifier'] = str(uuid.uuid4())
        self.msg_dict['ProjectNumber'] = project_number
        self.msg_dict['IsIdentifiableExtraction'] = False
        self.msg_dict['IsNoFilterExtraction'] = False
        self.msg_dict['JobSubmittedAt'] = time.strftime("%Y-%m-%dT%H:%MZ",time.gmtime())
        print("CTP input file will be %s/%s" % (yaml_dict['FileSystemOptions']['FileSystemRoot'], dicom_file_path))
        print("CTP output file will be %s/%s/%s" % (yaml_dict['FileSystemOptions']['ExtractRoot'], self.msg_dict['ExtractionDirectory'], self.msg_dict['OutputPath']))


class IsIdentifiable_Start_Message(smiMessage):
    """ SMI messages to send a file to IsIdentifiable """
    # eg. this is what CTP emitted, so is what IsIdentifiable will be given:
    # {"DicomFilePath":"InputDir/IM-0001-0019-copy.dcm",  "AnonymisedFileName":"outIM-0001-0019-copy.dcm",  "Status":0,
    # "ExtractionJobIdentifier":"bb1cbed5-a666-4307-a781-5b83926eaa81","ProjectNumber":"001","ExtractionDirectory":"extracted","JobSubmittedAt":"2019-12-19T10:49Z"}
    # However IsIdentifiable only needs to know where the anonymised file went
    # relative to the Extraction root directory.

    def __init__(self, yaml_dict, extraction_directory, anonymised_filename, project_number):
        super().__init__()
        self.msg_dict = {}
        self.msg_dict['DicomFilePath'] = 'not needed'
        self.msg_dict['ExtractionDirectory'] = extraction_directory
        self.msg_dict['AnonymisedFileName'] = anonymised_filename
        self.msg_dict['ExtractionJobIdentifier'] = str(uuid.uuid4())
        self.msg_dict['Status'] = 0
        self.msg_dict['ProjectNumber'] = project_number
        self.msg_dict['JobSubmittedAt'] = time.strftime("%Y-%m-%dT%H:%MZ",time.gmtime())


# ----------------------------------------------------------------------
def send_CTP_Start_Message(yaml_dict, input_file, extraction_dir, project_name):
    """ Sends a Message to exchange given by QueueName requesting that CTP anonymises a DICOM file.
    NOTE! Sends a message direct to CTP input queue, not to the exchange used by CohortExtractor. """

    msg = CTP_Start_Message(yaml_dict, input_file, extraction_dir, project_name)
    queueName = yaml_dict['CTPAnonymiserOptions']['AnonFileConsumerOptions']['QueueName']

    # Testing the new functions:
    #rabbit_prod = RabbitProducer(yaml_dict, exchange = "", routingKey = queueName)
    #rabbit_prod.open()
    #rabbit_prod.sendMessage(msg)
    #rabbit_prod.close()
    #return

    pika_connection = pika.BlockingConnection(_get_pika_connection_parameters(yaml_dict))
    pika_model = pika_connection.channel()

    pika_model.basic_publish(exchange = "", # rabbit will send direct to the named queue:
                            routing_key = queueName,
                            body = msg.to_json(),
                            properties = _get_pika_message_properties("myProducerName", 1234),
                            mandatory = True)
    print("Published %s" % msg.to_json())


# ----------------------------------------------------------------------
def get_CTP_Output_Message(yaml_dict, num_messages_to_read):
    """ Wait for a message from CTP in the queue defined by IsIdentifiableOptions|ValidateFileConsumerOptions|QueueName.
    Return its body as a dict. As we can wait for more than one message at a time we return an array of dicts.
    XXX bugs: acks the message before knowing whether IsIdentifiable can run successfully.
    """

    queueName = yaml_dict['IsIdentifiableOptions']['QueueName']
    result = []

    pika_connection = pika.BlockingConnection(_get_pika_connection_parameters(yaml_dict))
    pika_model = pika_connection.channel()

    # Get some messages and then stop even if more messages exist
    for method_frame, properties, body in pika_model.consume(queueName):

        # Convert message body from JSON string into Python dict
        msg_dict = json.loads(body)

        result.append(msg_dict)

        # Acknowledge the message
        pika_model.basic_ack(method_frame.delivery_tag)

        # Escape out of the loop after 1 messages
        if method_frame.delivery_tag == num_messages_to_read:
            break

    # Cancel the consumer and return any pending messages (Important!)
    requeued_messages = pika_model.cancel()
    #print('Requeued %i messages' % requeued_messages)

    # Close the channel and the connection
    pika_model.close()
    pika_connection.close()

    # Return the list of dictionaries
    return result

# ----------------------------------------------------------------------
def send_IsIdentifiable_Start_Message(yaml_dict, extraction_directory, anonymised_filename, project_number):
    """ Sends a Message to exchange given by QueueName requesting that CTP anonymises a DICOM file.
    NOTE! Sends a message direct to CTP input queue, not to the exchange used by CohortExtractor. """

    queueName = yaml_dict['IsIdentifiableOptions']['QueueName'] # XXX not in a sub-key yet

    pika_connection = pika.BlockingConnection(_get_pika_connection_parameters(yaml_dict))
    pika_model = pika_connection.channel()

    msg = IsIdentifiable_Start_Message(yaml_dict, extraction_directory, anonymised_filename, project_number)
    pika_model.basic_publish(exchange = "", # rabbit will send direct to the named queue:
                            routing_key = queueName,
                            body = msg.to_json(),
                            properties = _get_pika_message_properties("myProducerName", 1234),
                            mandatory = True)
    print("Published %s" % msg.to_json())


# ---------------------------------------------------------------------
# Simple test program to send a message to CTP and await a response message.

if __name__ == "__main__":
    if len(sys.argv) > 1:
        yaml_filename = sys.argv[1]
    else:
        yaml_filename = 'src/microservices/com.smi.microservices.ctpanonymiser/target/default.yaml'
    with open(yaml_filename) as fd:
        yaml_dict = yaml.load(fd)
    send_CTP_Start_Message(yaml_dict, "IM-0001-0019.dcm", "extracted", "001")
    print(get_CTP_Output_Message(yaml_dict, 1))
