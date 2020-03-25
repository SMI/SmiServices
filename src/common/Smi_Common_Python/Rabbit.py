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

# XXX bugs: cannot insert OriginalPublishTimestamp as a long.
# XXX currently only used for testing so contains debug statements.

import os   # for os.path.basename()
import yaml # for yaml.load()
import uuid # for uuid.uuid4()
import json # for json.dumps()
import time # for time.time() and time.gmtime() and time.strftime()
import pika # for rabbit API


#LOGGER = logging.getLogger(__name__)
#LOGGER.info('message')


# ----------------------------------------------------------------------
# These two internal functions create data structures specific to pika

def get_pika_connection_parameters(yaml_dict):
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

def get_pika_message_properties(producerExecutableName, producerProcessId):
    """ Returns populated message properties for publishing to RabbitMQ
        Parameters: this executable name and PID
        Returns: struct suitable for passing to the properties arg of pika_model.basic_publish """
    return pika.BasicProperties(
        delivery_mode=2,
        timestamp=round(time.time()),
        content_encoding="UTF-8",
        content_type="application/json",
        headers={
            "MessageGuid": str(uuid.uuid4()),
            "ProducerExecutableName": str(producerExecutableName),
            "ProducerProcessID": int(producerProcessId),
            "OriginalPublishTimestamp": int(timestamp), # XXX causes class java.lang.Integer cannot be cast to class java.lang.Long AND similar in .net because they expect long but pika sends int.
            "Parents": ""
        }
    )


# ----------------------------------------------------------------------
class smiMessage:
    """ SMI message base class.
    Currently only handles sending/output messages.
    Would need a from_json to handle receiving/input messages.
    Inherited by microservice-specific messages. """
    def __init__(self):
        msg_dict = {}

    def to_json(self):
        return json.dumps(self.msg_dict, default=lambda o: o.__dict__, sort_keys=True, indent=4)


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

    queueName = yaml_dict['CTPAnonymiserOptions']['ExtractFileConsumerOptions']['QueueName']

    pika_connection = pika.BlockingConnection(get_pika_connection_parameters(yaml_dict))
    pika_model = pika_connection.channel()

    msg = CTP_Start_Message(yaml_dict, input_file, extraction_dir, project_name)
    pika_model.basic_publish(exchange = "", # rabbit will send direct to the named queue:
                            routing_key = queueName,
                            body = msg.to_json(),
                            properties = get_pika_message_properties("myProducerName", 1234),
                            mandatory = True)
    print("Published %s" % msg.to_json())


# ----------------------------------------------------------------------
def get_CTP_Output_Message(yaml_dict, num_messages_to_read):
    """ Wait for a message from CTP in the queue defined by IsIdentifiableOptions|ValidateFileConsumerOptions|QueueName.
    Return its body as a dict. As we can wait for more than one message at a time we return an array of dicts.
    XXX bugs: acks the message before knowing whether IsIdentifiable can run successfully.
    """

    queueName = yaml_dict['IsIdentifiableOptions']['ValidateFileConsumerOptions']['QueueName']
    result = []

    pika_connection = pika.BlockingConnection(get_pika_connection_parameters(yaml_dict))
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

    pika_connection = pika.BlockingConnection(get_pika_connection_parameters(yaml_dict))
    pika_model = pika_connection.channel()

    msg = IsIdentifiable_Start_Message(yaml_dict, extraction_directory, anonymised_filename, project_number)
    pika_model.basic_publish(exchange = "", # rabbit will send direct to the named queue:
                            routing_key = queueName,
                            body = msg.to_json(),
                            properties = get_pika_message_properties("myProducerName", 1234),
                            mandatory = True)
    print("Published %s" % msg.to_json())


# ---------------------------------------------------------------------
# Simple test program to send a message to CTP and await a response message.

if __name__ == "__main__":
    yaml_filename = 'src/microservices/com.smi.microservices.ctpanonymiser/target/default.yaml'
    with open(yaml_filename) as fd:
        yaml_dict = yaml.load(fd)
    send_CTP_Start_Message(yaml_dict, "IM-0001-0019.dcm", "extracted", "001")
    print(get_CTP_Output_Message(yaml_dict, 1))
