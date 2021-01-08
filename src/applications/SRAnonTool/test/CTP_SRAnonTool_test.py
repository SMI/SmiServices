#!/usr/bin/env python3

# Given a real DICOM Structured Report extract the text using our CTP_DicomToText.py
# then create a fake XML file to simulate the running of the SemEHR anonymiser
# then run our CTP_XMLToDicom.py to reconstruct a DICOM file.
# Whilst this does make assumptions about the behaviour of SemEHR, it does
# allow us to test the text extraction, redaction and DICOM recreation all work.

import os
import pydicom
import re
import shutil
import sys
os.environ['PYTHONPATH'] = '../../../common'
sys.path.append('../../../common')
from Smi_Common_Python import DicomText, Knowtator

source_dcm = 'report10.dcm'
tmp_file = '/tmp/CTP_SRAnonTool_test_report10.tmp'
txt_file = '/tmp/CTP_SRAnonTool_test_report10.txt'
xml_file = '/tmp/CTP_SRAnonTool_test_report10.txt.knowtator.xml'
anon_dcm_file = '/tmp/CTP_SRAnonTool_test_report10.dcm'
test_before = '/tmp/CTP_SRAnonTool_test_report10.txt.before'
test_after  = '/tmp/CTP_SRAnonTool_test_report10.txt.after'

yaml1 = '/home/arb/src/SmiServices/data/microserviceConfigs/default.yaml'
binpath = '..'

# Copy the input DICOM to the output filename, to simulate running CTP
# (we don't redact any tags but that doesn't matter for this test).
shutil.copy(source_dcm, anon_dcm_file)
with open(source_dcm, 'rb') as fdin:
	with open(anon_dcm_file, 'wb') as fdout:
		fdout.write(fdin.read())

# Extract the text which would be input to SemEHR
os.system(f"{binpath}/CTP_DicomToText.py -y {yaml1} -i {source_dcm}  -o {tmp_file}")

# Fake the output from SemEHR, both txt and xml.
# Create the text file by stripping off the headers:
fdin = open(tmp_file, 'r')
fdout = open(txt_file, 'w')
in_text = False
in_cs = False
for line in fdin:
	if re.match(r'^\[\[Text\]\]', line):
		in_text = True
		continue
	if re.match(r'^\[\[ContentSequence\]\]', line):
		in_cs = True
		continue
	if re.match(r'^\[\[EndText\]\]', line):
		in_text = False
		continue
	if re.match(r'^\[\[EndContentSequence\]\]', line):
		in_cs = False
		continue
	if not in_text and not in_cs:
		continue
	fdout.write(line)
fdin.close()
fdout.close()
# Create the XML dict by finding the pattern in the text:
with open(txt_file, 'r') as fd:
	txt = fd.read()
pattern = 'Baker'
xml_dict = [{ 'start_char': m.start(), 'end_char': m.end(), 'text': pattern } for m in re.finditer(pattern, txt)]
# Create the XML file:
xml_str = Knowtator.dict_to_annotation_xml_string(xml_dict)
with open(xml_file, 'w') as fd:
	fd.write(xml_str)

# Now convert the txt,xml back into a redacted DICOM file:
os.system(f"{binpath}/CTP_XMLToDicom.py -y {yaml1} -i {source_dcm} -x {xml_file} -o {anon_dcm_file}")

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
manually_redacted = re.sub(pattern, 'X'.rjust(len(pattern), 'X'), before_text)
with open(test_before, 'w') as fd: fd.write(manually_redacted)
with open(test_after, 'w') as fd: fd.write(after_text)
rc = os.system('diff -wB %s %s' % (test_before, test_after))
if rc == 0:
	print('SUCCESS')
	rc = 0
else:
	print('FAIL')
	rc = 1

# Tidy up (ignore errors)
try:
    os.remove(tmp_file)
    os.remove(txt_file)
    os.remove(xml_file)
    os.remove(anon_dcm_file)
    os.remove(test_before)
    os.remove(test_after)
except:
	pass

exit(rc)