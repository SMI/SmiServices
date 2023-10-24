#!/usr/bin/env python3

# Usage: [-s semehr_dir] [-d file.dcm] [-p pattern] [-y default.yaml]

# Given a real DICOM Structured Report extract the text using our CTP_DicomToText.py
# then create a fake XML file to simulate the running of the SemEHR anonymiser
# then run our CTP_XMLToDicom.py to reconstruct a DICOM file.
# Whilst this does make assumptions about the behaviour of SemEHR, it does
# allow us to test the text extraction, redaction and DICOM recreation all work.

import argparse
import logging
import os
from os.path import join, abspath, dirname
import pydicom
import re
import shutil
import sys
from SmiServices import DicomText

# Configurable:
semehr_dir = '/opt/semehr'
source_dcm = join(abspath(dirname(__file__)), 'report10html.dcm')
fake_pattern = 'Baker'  # This appears in the test document so anonymise it
# yaml1 = '/home/arb/src/SmiServices/data/microserviceConfigs/default.yaml'
# yaml1 = os.path.join(os.environ['SMI_ROOT'], 'config', 'smi_dataExtract.yaml')
yaml1 = join(abspath(dirname(__file__)), "../../../../data/microserviceConfigs/default.yaml")
binpath = join(abspath(dirname(__file__)), '..')

parser = argparse.ArgumentParser(description='SemEHR-Anonymiser-test')
parser.add_argument('-s', dest='semehr_dir', action="store", help=f'root path for semehr, default {semehr_dir}')
parser.add_argument('-d', dest='dcm', action="store", help=f'DICOM file to anonymise, default {source_dcm}')
parser.add_argument('-p', dest='pattern', action="store", help=f'pattern to redact, default {fake_pattern}')
parser.add_argument('-y', dest='yaml', action="store", help=f'default.yaml, default {yaml1}')
args = parser.parse_args()
if args.semehr_dir:
	semehr_dir = args.semehr_dir
if args.dcm:
	source_dcm = args.dcm
if args.pattern:
	fake_pattern = args.pattern
if args.yaml:
	yaml1 = args.yaml
logging.basicConfig(level = logging.INFO)

# Intermediate files:
anon_dcm_file = 'report10html.anon.dcm'
source_txt_file = join(semehr_dir, 'data', 'input_docs', 'report10html.txt')
txt_file = join(semehr_dir, 'data', 'anonymised', 'report10html.txt')
xml_file = join(semehr_dir, 'data', 'anonymised', 'report10html.txt.knowtator.xml')
test_before = '/tmp/CTP_SRAnonTool_test_report10html.txt.before'
test_after  = '/tmp/CTP_SRAnonTool_test_report10html.txt.after'

# Copy the input DICOM to the output filename, to simulate running CTP
# (we don't redact any tags but that doesn't matter for this test).
shutil.copy(source_dcm, anon_dcm_file)
with open(source_dcm, 'rb') as fdin:
	with open(anon_dcm_file, 'wb') as fdout:
		fdout.write(fdin.read())

# Extract the text which would be input to SemEHR
logging.info('Extracting text from %s using CTP_DicomToText.py' % source_dcm)
os.system(f"{binpath}/CTP_DicomToText.py -y {yaml1} -i {source_dcm}  -o {source_txt_file}")

# Fake the output from SemEHR, both txt and xml.
logging.info('Running a fake anonymiser to get XML %s' % xml_file)
os.system(join(abspath(dirname(__file__)), f"clinical_doc_wrapper.py -s {semehr_dir}"))

# Now convert the txt,xml back into a redacted DICOM file:
logging.info('Redacting using XML into %s' % anon_dcm_file)
os.system(f"{binpath}/CTP_XMLToDicom.py -y {yaml1} -i {source_dcm} -x {xml_file} -o {anon_dcm_file}")

# Extract the text from the DICOM file to the screen if possible
if shutil.which('jq') and shutil.which('dcm2json'):
	logging.info('Text in new DICOM file:')
	os.system("dcm2json %s | jq '..|select(.vr==\"UT\")?|.Value[0]'" % anon_dcm_file)

# Finally compare the two text strings
before = DicomText.DicomText(source_dcm)
before.parse()
before_text = before.text()

after  = DicomText.DicomText(anon_dcm_file)
after.parse()
after_text = after.text()

#print('ORIGINAL TEXT:\n%s' % before_text)
#print('REDACTED TEXT:\n%s' % after_text)

# Compare the text before and after redaction.
# Manually replace the pattern with X so they should be identical.
# We need to use diff to compare the output because it can ignore changes in whitespace/blank lines.
manually_redacted = re.sub(fake_pattern, 'X'.rjust(len(fake_pattern), 'X'), before_text)
with open(test_before, 'w') as fd: fd.write(manually_redacted)
with open(test_after, 'w') as fd: fd.write(after_text)
rc = os.system('diff -wB --ignore-matching="\\[\\[" %s %s' % (test_before, test_after))
if rc == 0:
	print('SUCCESS')
	rc = 0
else:
	print('FAIL')
	rc = 1

# Tidy up (ignore errors)
if rc == 0:
	try:
	    os.remove(source_txt_file)
	    os.remove(txt_file)
	    os.remove(xml_file)
	    os.remove(anon_dcm_file)
	    os.remove(test_before)
	    os.remove(test_after)
	except:
		pass

exit(rc)
