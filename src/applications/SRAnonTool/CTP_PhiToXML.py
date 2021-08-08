#!/usr/bin/env python3
# Convert from the "Phi" output of the SemEHR anonymiser into the Knowtator XML format.
# Hopefully this is only temporary until the knowtator XML output is fixed,
# because the phi output lacks some of the anonymisations (the 'Q' ones).
#
# Usage:
# CTP_PhiToXML.py -p file.phi [-o output dir]
# where file.phi is the phi output file as configured in conf/anonymisation_task.json
# extracted_phi = "dir/file.phi"
# The output goes into the same directory, unless changed with -o.
# Requires the SmiServices python package in a virtualenv.
#
# Input format is like this:
# [
#  {
#    "doc": "in.dcm",
#    "pos": 1408,
#    "start": 1408,
#    "type": "phone",
#    "sent": "20160208124216.000000"
#  },
# Output format is like this (start and end attributes have different names):
#  <annotation>
#  <span end="1183" start="1173"/>
#  <spannedText>T CAMPBELL</spannedText>
# </annotation>

import argparse
import json
import os
import sys
from SmiServices import Knowtator

parser = argparse.ArgumentParser(description = 'Convert PHI to Knowtator XML')
parser.add_argument('-p', dest='phi', help='path to phi file', required=True)
parser.add_argument('-o', dest='outdir', help='output directory if not same as phi directory')
args = parser.parse_args()

phi_filename = args.phi
out_dir = args.outdir if args.outdir else os.path.dirname(phi_filename)

# What does an empty knowtator.xml document need to look like?
empty_knowtator_xml_document_string = '<?xml ?>\n<annotations>\n</annotations>\n'


# Function to write a string to a file, the filename will have .knowtator.xml added.
def write_knowtator_xml_doc(filename_prefix, xml_string):
    """ Append the .knowtator.xml suffix and write the string to the file """
    filename = '%s.knowtator.xml' % (filename_prefix)
    print('Converted %s to file %s' % (phi_filename, filename))
    with open(filename, 'w') as fd:
        print(xml_string, file=fd)


# Open the Phi file and load the JSON content
with open(phi_filename) as fd:
    phi_json = json.load(fd)

# Collect the set of document names
list_of_docs = set([ii['doc'] for ii in phi_json])

# For each document, convert the attribute names and write as xml
for doc in list_of_docs:
    renamed_json = [{'start_char':ii['start'], 'end_char':ii['start']+len(ii['sent']), 'text':ii['sent']} for ii in phi_json if ii['doc'] == doc]
    renamed_json = sorted(renamed_json, key = lambda x: x['start_char'])
    xml_string = Knowtator.dict_to_annotation_xml_string(renamed_json) if len(renamed_json) else empty_knowtator_xml_document_string
    write_knowtator_xml_doc(os.path.join(out_dir, doc), xml_string)

exit(0)
