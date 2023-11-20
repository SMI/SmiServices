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
import logging, logging.handlers
import xml.etree.ElementTree    # untangle and xmltodict not available in NSH
from deepmerge import Merger    # for deep merging dictionaries
import pydicom
import random
import yaml
sys.path.append('../../common/') # if we are in the application directory
sys.path.append('src/common')    # if we are in the root of the repo
if 'SMI_ROOT' in os.environ:     # $SMI_ROOT/lib/python3
    sys.path.append(os.path.join(os.environ['SMI_ROOT'], 'lib', 'python3'))
from SmiServices import Knowtator
from SmiServices import Dicom
from SmiServices import DicomText


# ---------------------------------------------------------------------
if __name__ == "__main__":

    # Parse command line arguments
    parser = argparse.ArgumentParser(description='Anon-to-SR')
    parser.add_argument('-y', dest='yamlfile', action="append", help='path to yaml extract config file (can be used more than once)')
    parser.add_argument('-i', dest='input_dcm', action="store", help='Path to raw DICOM file')
    parser.add_argument('-x', dest='input_xml', action="store", help='Path to annotation XML file')
    parser.add_argument('-o', dest='output_dcm', action="store", help='Path to anonymised DICOM file to have redacted text inserted')
    parser.add_argument('--replace-html', action="store", help='replace HTML with a character, default is dot (.), or "squash" to eliminate')
    parser.add_argument('--replace-newlines', action="store", help='replace carriage returns and newlines with a character (e.g. a space) or "squash" to eliminate')
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
            cfg_dict = Merger([(list, ["append"]),(dict, ["merge"])],["override"],["override"]).merge(cfg_dict, yaml.safe_load(fd))

    log_dir = cfg_dict['LoggingOptions']['LogsRoot']
    root_dir = cfg_dict['FileSystemOptions']['FileSystemRoot']
    extract_dir = cfg_dict['FileSystemOptions']['ExtractRoot']

    # ---------------------------------------------------------------------
    # Now we know the LogsRoot we can set up logging
    log_file_handler = logging.handlers.RotatingFileHandler(filename = os.path.join(log_dir,'SRAnonymiser.log'), maxBytes=64*1024*1024, backupCount=9)
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
    # If the file is a DICOM then DicomText has options to change the output format.
    # These are passed to the DicomText and StructuredReport constructors.
    DicomTextArgs = {
        #'include_header' : True,
        #'replace_HTML_entities' : True,
        'replace_HTML_char' : '.',
        'replace_newline_char' : '\n'
    }
    if args.replace_html:
        DicomTextArgs['replace_HTML_char'] = args.replace_html
        if args.replace_html == "squash":
            DicomTextArgs['replace_HTML_char'] = ''
    if args.replace_newlines:
        DicomTextArgs['replace_newline_char'] = args.replace_newlines
        if args.replace_newlines == "squash":
            DicomTextArgs['replace_newline_char'] = ''

    # ---------------------------------------------------------------------
    # Read the original DICOM file and parse the original text
    dicomtext = DicomText.DicomText(args.input_dcm, **DicomTextArgs)
    dicomtext.parse()

    # Read the annotated XML file
    xmlroot = xml.etree.ElementTree.parse(args.input_xml).getroot()
    xmldictlist = Knowtator.annotation_xml_to_dict(xmlroot)
    #if xmldictlist == []:
    #    print('WARNING: empty document in {}'.format(args.input_xml))
    #for annot in xmldictlist:
    #    print('REMOVE {} from DICOM at {}'.format(annot['text'], annot['start_char']))

    # Redact the annotations from the DICOM
    dicomtext.redact(xmldictlist)
    #print('ORIG %s' % dicomtext.text())
    #print('THE REDACTED TEXT IS:\n%s' % dicomtext.redacted_text())

    dicomtext.write_redacted_text_into_dicom_file(args.output_dcm)
    #print(f'dcm2json {redacted_dcmname} | jq \'..|select(.vr=="UT")?|.Value|.[]\'')
    logging.info(f'Wrote {args.output_dcm}')

    exit(0)
