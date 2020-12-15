#!/usr/bin/env python3

# Convert a DICOM file into plain text in a suitable format
# for passing into the anonymisation part of SemEHR.
# Usage: -y default.yaml -i input -o output
#  -y = path to the default.yaml file to get FileSystemRoot and Mongo
#  -i = input DICOM file, full path or relative to FileSystemRoot,
#       or if not found then looked up in the MongoDB.
#  -o = output filename or directory for the plain text file
# Needs both dataLoad and dataExtract yaml files because Mongo is
# defined in the former and the rest in the latter.

import argparse
import logging
import os
import sys
import json
import yaml
import pydicom
import xml.etree.ElementTree    # untangle and xmltodict not available in NSH
from deepmerge import Merger    # for deep merging dictionaries
sys.path.append('../../common/') # if we are in the application directory
sys.path.append('src/common')    # if we are in the root of the repo
if 'SMI_ROOT' in os.environ:     # $SMI_ROOT/lib/python3
    sys.path.append(os.path.join(os.environ['SMI_ROOT'], 'lib', 'python3'))
from Smi_Common_Python import Mongo
from Smi_Common_Python import Rabbit
from Smi_Common_Python import Dicom
from Smi_Common_Python import DicomText
from Smi_Common_Python import StructuredReport as SR

# ---------------------------------------------------------------------

def extract_mongojson(mongojson, output):
    """ Called by extract_mongojson_file
    to parse the JSON from Mongo and write to output.
    mongojson - the DICOM in JSON format.
    output - can be a directory or a filename.
    """
    if os.path.isdir(output):
        filename = mongojson['SOPInstanceUID'] + '.txt'
        output = os.path.join(output, filename)
    with open(output, 'w') as fd:
        SR.SR_parse(mongojson, filename, fd)    
    logging.info(f'Wrote {output}')


def extract_mongojson_file(input, output):
    """ Read MongoDB data in JSON format from input file
    convert to output, which can be a filename or directory.
    """
    with open(input, 'r') as fd:
        mongojson = json.load(fd)
    extract_mongojson(mongojson, output)


# ---------------------------------------------------------------------

def extract_dicom_file(input, output):
    """ Extract text from a DICOM file input
    into the output, which can be a filename,
    or a directory in which case the file is named by SOPInstanceUID.
    """
    # Old method using hand-crafted code
    #dicom_raw = pydicom.dcmread(input)
    #dicom_raw_json = dicom_raw.to_json_dict(None, 0)
    #SR.SR_parse(dicom_raw_json, 'TEST_DICOM_NAME', sys.stdout)

    # New method using DicomText class
    dicomtext = DicomText.DicomText(input)
    dicomtext.parse()

    if os.path.isdir(output):
        filename = dicomtext.SOPInstanceUID() + '.txt'
        output = os.path.join(output, filename)
    with open(output, 'w') as fd:
        fd.write(dicomtext.text())
    logging.info(f'Wrote {output}')


# ---------------------------------------------------------------------

def extract_file(input, output):
    """ If it's a readable DICOM file then extract it
    otherwise try to find it in MongoDB.
    """
    try:
        pydicom.dcmread(input)
        is_dcm = True
    except:
        is_dcm = False

    if is_dcm:
        extract_dicom_file(input, output)
    else:
        extract_mongojson_file(input, output)



# ---------------------------------------------------------------------
if __name__ == '__main__':

    # Parse command line arguments
    parser = argparse.ArgumentParser(description='SR-to-Anon')
    parser.add_argument('-y', dest='yamlfile', action="append", help='path to yaml config file (can be used more than once)')
    parser.add_argument('-i', dest='input', action="store", help='SOPInstanceUID or path to raw DICOM file from which text will be redacted')
    parser.add_argument('-o', dest='output_dir', action="store", help='path to directory where extracted text will be written')
    args = parser.parse_args()
    if not args.input:
        parser.print_help()
        exit(1)
    if not args.output_dir:
        args.output_dir = '.'

    cfg_dict = {}
    if not args.yamlfile:
        args.yamlfile = [os.path.join(os.environ['SMI_ROOT'], 'configs', 'smi_dataExtract.yaml')]
    for cfg_file in args.yamlfile:
        with open(cfg_file, 'r') as fd:
            # Merge all the yaml dicst into one
            cfg_dict = Merger([(list, ["append"]),(dict, ["merge"])],["override"],["override"]).merge(cfg_dict, yaml.load(fd))

    mongo_host = cfg_dict['MongoDatabases']['DicomStoreOptions']['HostName']
    mongo_user = cfg_dict['MongoDatabases']['DicomStoreOptions']['UserName']
    mongo_pass = cfg_dict['MongoDatabases']['DicomStoreOptions']['Password']
    mongo_db   = cfg_dict['MongoDatabases']['DicomStoreOptions']['DatabaseName']

    log_dir = cfg_dict['LogsRoot']
    root_dir = cfg_dict['FileSystemOptions']['FileSystemRoot']
    extract_dir = cfg_dict['FileSystemOptions']['ExtractRoot']

    # ---------------------------------------------------------------------
    # Now we know the LogsRoot we can set up logging
    log_file_handler = logging.FileHandler(filename = os.path.join(log_dir,'SRAnonymiser.log'))
    log_stdout_handler = logging.StreamHandler(sys.stdout)
    logging.basicConfig(level=logging.INFO, handlers=[log_file_handler, log_stdout_handler],
        format='[%(asctime)s] {%(filename)s:%(lineno)d} %(levelname)s - %(message)s')

    # ---------------------------------------------------------------------
    if os.path.isfile(args.input):
        # actual path to DICOM
        extract_file(args.input, args.output_dir)
    elif os.path.isfile(os.path.join(root_dir, args.input)):
        # relative to FileSystemRoot
        extract_file(os.path.join(root_dir, args.input), args.output_dir)
    elif os.path.isdir(args.input):
        # Recurse directory
        for root, dirs, files in os.walk(args.input, topdown=False):
            for name in files:
                extract_file(os.path.join(root, name), args.output_dir)
    else:
        # could be a SOPInstanceUID in Mongo
        # except SOPInstanceUID is not in the Mongo index,
        # only the DicomFilePath.
        mongodb = Mongo.SmiPyMongoCollection(mongo_host)
        mongodb.setImageCollection('SR')
        mongojson = mongodb.DicomFilePathToJSON(args.input)
        extract_mongojson(mongojson, output_dir)
