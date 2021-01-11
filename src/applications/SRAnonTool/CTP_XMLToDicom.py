#!/usr/bin/env python3

# Program to replace (redact) text in a DICOM file
# by reading a list of annotations from an XML file
# saying which character positions to redact.
# Has to recurse through the DICOM regenerating the text
# in the same way it was originally generated,
# calculating the offset as it goes,
# and replacing sections which match annotations.
# Usage: -y default.yaml -i input.dcm -x input.xml -o output.dcm

import os, sys
import argparse
import logging
import xml.etree.ElementTree    # untangle and xmltodict not available in NSH
from deepmerge import Merger    # for deep merging dictionaries
import pydicom
import random
import yaml
sys.path.append('../../common/') # if we are in the application directory
sys.path.append('src/common')    # if we are in the root of the repo
if 'SMI_ROOT' in os.environ:     # $SMI_ROOT/lib/python3
    sys.path.append(os.path.join(os.environ['SMI_ROOT'], 'lib', 'python3'))
from Smi_Common_Python import Knowtator
from Smi_Common_Python import Dicom
from Smi_Common_Python import DicomText


# ---------------------------------------------------------------------
if __name__ == "__main__":

    # Parse command line arguments
    parser = argparse.ArgumentParser(description='Anon-to-SR')
    parser.add_argument('-y', dest='yamlfile', action="append", help='path to yaml extract config file (can be used more than once)')
    parser.add_argument('-i', dest='input_dcm', action="store", help='Path to raw DICOM file')
    parser.add_argument('-x', dest='input_xml', action="store", help='Path to annotation XML file')
    parser.add_argument('-o', dest='output_dcm', action="store", help='Path to anonymised DICOM file to have redacted text inserted')
    args = parser.parse_args()
    if not args.input_dcm or not args.input_xml or not args.output_dcm:
        parser.print_help()
        exit(1)

    cfg_dict = {}
    if not args.yamlfile:
        args.yamlfile = [os.path.join(os.environ['SMI_ROOT'], 'configs', 'smi_dataExtract.yaml')]
    for cfg_file in args.yamlfile:
        with open(cfg_file, 'r') as fd:
            # Merge all the yaml dicst into one
            cfg_dict = Merger([(list, ["append"]),(dict, ["merge"])],["override"],["override"]).merge(cfg_dict, yaml.load(fd))

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
    # Check all files exist
    if not os.path.exists(args.input_dcm):
        logging.error('ERROR: no such file named {}'.format(args.input_dcm))
        exit(1)
    if not os.path.exists(args.input_xml):
        logging.error('ERROR: no such file named {}'.format(args.input_xml))
        exit(1)
    if not os.path.exists(args.output_dcm):
        logging.error('ERROR: no such file named {} (redacted text is written into this so it must exist)'.format(args.output_dcm))
        exit(1)

    # ---------------------------------------------------------------------
    # Read the original DICOM file and parse the original text
    dicomtext = DicomText.DicomText(args.input_dcm)
    dicomtext.parse()

    # Read the annotated XML file
    xmlroot = xml.etree.ElementTree.parse(args.input_xml).getroot()
    xmldictlist = Knowtator.annotation_xml_to_dict(xmlroot)
    if xmldictlist == []:
        print('WARNING: empty document in {}'.format(xmlfilename))
    #for annot in xmldictlist:
    #    print('REMOVE {} from DICOM at {}'.format(annot['text'], annot['start_char']))

    # Redact the annotations from the DICOM
    dicomtext.redact(xmldictlist)
    #print('ORIG %s' % dicomtext.text())
    #print('THE REDACTED TEXT IS:\n%s' % dicomtext.redacted_text())

    dicomtext.write_redacted_text_into_dicom_file(args.output_dcm)
    #print(f'dcm2json {redacted_dcmname} | jq \'..|select(.vr=="UT")?|.Value|.[]\'')

    exit(0)
