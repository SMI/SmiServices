#!/usr/bin/env python3

# This stub program simulates the effect of running
#   ${semehr_dir}/CogStack-SemEHR/analysis/clinical_doc_wrapper.py
#
# NOTE!  This program uses python3 but the real clinical_doc_wrapper
# still uses python2 so this stub does not verify that python2 works.
#
# It expects the input documents (from CTP_DicomToText.py) in
#   ${semehr_dir}/data/input_docs/
# and writes output into
#   ${semehr_dir}/data/anonymised/
#
#   ${semehr_dir} is typically /opt/semehr

import glob
import logging
import os
import re
from Smi_Common_Python import Knowtator

semehr_root_dir = '/opt/semehr'
input_dir = os.path.join(semehr_root_dir, 'data/input_docs')
output_dir = os.path.join(semehr_root_dir, 'data/anonymised')
fake_pattern = 'Baker'  # This appears in the test document so anonymise it

if not os.path.isdir(input_dir):
	logging.error(f'no such input directory {input_dir}')
	exit(1)
if not os.path.isdir(output_dir):
	logging.error(f'no such output directory {output_dir}')
	exit(1)


def fake_anonymise(doc_filename):
	""" Fake the output from SemEHR, both txt and xml.
	Creates the text file by stripping off the headers.
	Given a filename (no path) read input_dir/doc_filename
	and write to two files:
	output_dir/doc_filename
	output_dir/doc_filename.knowtator.xml
	"""
	tmp_file = os.path.join(input_dir, doc_filename)
	txt_file = os.path.join(output_dir, doc_filename)
	xml_file = os.path.join(output_dir, doc_filename+'.knowtator.xml')
	logging.debug(f'Fake-anonymising {doc_filename} -> {txt_file}')
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
	xml_dict = [{ 'start_char': m.start(), 'end_char': m.end(), 'text': fake_pattern } for m in re.finditer(fake_pattern, txt)]
	# Create the XML file:
	xml_str = Knowtator.dict_to_annotation_xml_string(xml_dict)
	with open(xml_file, 'w') as fd:
		fd.write(xml_str)


	return


def main():
	logging.basicConfig(level=logging.DEBUG)
	for doc_filename in glob.glob(os.path.join(input_dir, '*')):
		fake_anonymise(os.path.basename(doc_filename))

main()

exit(0)


