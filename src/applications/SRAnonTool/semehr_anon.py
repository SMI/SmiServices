#!/usr/bin/env python3
# Anonymise a text file or a directory of text files
# Usage: semehr_anon.sh -i input_dir -o output_dir
#    or: semehr_anon.sh -i input_file -o output_file
#          -s path to the parent of CogStack-SemEHR dir.
#          --spacy to use SpaCy named entity recogniser.
#          --xml to write .knowtator.xml files.
# To anonymise *DICOM* files you need CTP_SRAnonTool.sh
# (which uses CTP_DicomToText, this script, and CTP_XMLToDicom).
# NOTE: this script has superceded semehr_anon.sh.

import argparse, json, logging, re, sys, os, glob
import shutil # for copyfile
import tempfile # for TemporaryDirectory
import subprocess # for run
from SmiServices import Knowtator
from logging import handlers

# Configuration
use_spacy = False
empty_knowtator_xml_document_string = '<?xml version="1.0" ?>\n<annotations>\n</annotations>\n'


def anonymise_dir(input_dir, output_dir, semehr_dir, semehr_anon_cfg_file, write_xml = False):

    input_dir = os.path.abspath(input_dir)
    output_dir = os.path.abspath(output_dir)

    cfg_file = os.path.join(output_dir, 'anonymisation_task.json')
    phi_file = os.path.join(output_dir, 'anonymiser.phi')
    log_file = os.path.join(output_dir, 'anonymiser.log')

    # Create a config file in the output directory
    with open(semehr_anon_cfg_file) as fd:
        cfg_json = json.load(fd)
    cfg_json['text_data_path'] = input_dir
    cfg_json['anonymisation_output'] = output_dir
    cfg_json['extracted_phi'] = phi_file
    cfg_json['grouped_phi_output'] = '/dev/null'
    cfg_json['logging_file'] = log_file
    cfg_json['annotation_mode'] = False
    #cfg_json['number_threads'] = 0  # a bug in CPython causes hang/deadlock trying to acquire lock in logging __init__.py ?
    cfg_json['use_spacy'] = use_spacy
    with open(cfg_file, 'w') as fd:
        print(json.dumps(cfg_json), file=fd)

    # Run SemEHR anonymiser
    cur_dir = os.getcwd()
    os.chdir(os.path.join(semehr_dir, 'CogStack-SemEHR', 'anonymisation'))
    logging.info('Running anonymiser from %s to %s' % (input_dir, output_dir))
    result = subprocess.run(['python3', 'anonymiser.py', cfg_file], stdout=subprocess.PIPE, stderr=subprocess.PIPE)
    os.chdir(cur_dir)
    if result.returncode:
        logging.error('ERROR: SemEHR anonymiser failed: "%s"' % (result.stdout.decode('utf-8') + result.stderr.decode('utf-8')))
        # XXX should return early?

    # Read the JSON file "phi" to redact the words
    with open(phi_file) as fd:
        phi_json = json.load(fd)

    # Collect the set of document names
    # Can either use the list of input files (if you want every input file to have an output file)
    # or the list of documents which were redacted (if you don't want failed docs in the output dir)
    list_of_docs = set([ii['doc'] for ii in phi_json]) # list of anonymised documents
    if write_xml:
        list_of_docs = next(os.walk(input_dir))[2]     # list of original filenames
    logging.info('Redacting %d files in %s' % (len(list_of_docs), output_dir))

    # For each document, reads the output document and fully anonymise it using the 'phi' file
    # (The output document is almost anonymised but not necessarily fully as extra annotations are in the phi file.
    # Compared to the input document, the output document also has had the headers removed).
    for doc in list_of_docs:
        renamed_dict = [{'start_char':ii['start'], 'end_char':ii['start']+len(ii['sent']), 'text':ii['sent']} for ii in phi_json if ii['doc'] == doc]
        renamed_dict = sorted(renamed_dict, key = lambda x: x['start_char'])
        xml_string = Knowtator.dict_to_annotation_xml_string(renamed_dict) if len(renamed_dict) else empty_knowtator_xml_document_string

        # If there's no record of doc in phi then it wasn't anonymised
        # so we can either copy it (might contain PII!) or create an empty one from /dev/null
        if not renamed_dict:
            input_file = '/dev/null' # '/dev/null' OR os.path.join(input_dir, doc)
        else:
            input_file = os.path.join(output_dir, doc)
        with open(input_file, 'r') as fd:
            text = fd.read()

        for annot in renamed_dict:
            if annot['end_char'] > annot['start_char']:
                text = text[:annot['start_char']] + 'X'.rjust(annot['end_char']-annot['start_char'],'X') + text[annot['end_char']:]

        output_file = os.path.join(output_dir, doc)
        logging.debug('Redacting %s' % doc)
        with open(output_file, 'w') as fd:
            fd.write(text)
        # Write the XML file
        output_file += '.knowtator.xml'
        if write_xml:
            with open(output_file, 'w') as fd:
                fd.write(xml_string)

    # Tidy up
    os.remove(cfg_file)
    os.remove(phi_file)
    os.remove(log_file)
    return


def anonymise_file(input_file, output_file, semehr_dir, semehr_anon_cfg_file, write_xml = False):
    # The anonymiser works on whole directories so
    # create temporary input/output directories and copy the file there.
    input_file = os.path.abspath(input_file)
    output_file = os.path.abspath(output_file)
    input_dir = tempfile.TemporaryDirectory()
    output_dir = tempfile.TemporaryDirectory()
    shutil.copyfile(input_file, os.path.join(input_dir.name, os.path.basename(input_file)))
    anonymise_dir(input_dir.name, output_dir.name, semehr_dir, semehr_anon_cfg_file, write_xml=write_xml)
    shutil.copyfile(os.path.join(output_dir.name, os.path.basename(input_file)), output_file)
    if write_xml:
        xml_src = os.path.join(output_dir.name, os.path.basename(input_file) + '.knowtator.xml')
        xml_dest = output_file + '.knowtator.xml'
        if os.path.isfile(xml_src):
            shutil.copyfile(xml_src, xml_dest)
    input_dir.cleanup()
    output_dir.cleanup()


def main():
    global use_spacy

	# Configure logging
    if not os.environ.get('SMI_LOGS_ROOT'): raise Exception('Environment variable SMI_LOGS_ROOT must be set')
    log_dir = os.path.expandvars('$SMI_LOGS_ROOT')
    log_file = os.path.join(log_dir, os.path.basename(sys.argv[0]) + '.log')
    file_handler = logging.handlers.RotatingFileHandler(filename=log_file, maxBytes=256*1024*1024, backupCount=9)
    stdout_handler = logging.StreamHandler(sys.stdout)
    handlers = [file_handler, stdout_handler]
    logging.basicConfig(level=logging.DEBUG, handlers=handlers,
        format='[%(asctime)s] {%(filename)s:%(lineno)d} %(levelname)s - %(message)s')

    # Parse command line arguments
    parser = argparse.ArgumentParser(description='Redact text given knowtator XML')
    parser.add_argument('-i', dest='input', action="store", help='directory of text, or filename of one text file')
    parser.add_argument('-o', dest='output', action="store", help='path to output filename or directory where redacted text files will be written')
    parser.add_argument('--xml', dest='write_xml', action="store_true", help='write knowtator.xml in output directory for all input files')
    parser.add_argument('-s', dest='semehr_dir', action="store", help='path to parent of CogStack-SemEHR directory')
    parser.add_argument('--spacy', dest='spacy', action='store_true', help='use SpaCy to identify names')
    args = parser.parse_args()
    if not args.input:
        parser.print_help()
        exit(1)

    if args.spacy:
    	use_spacy = True # XXX global

    # Default SemEHR directory for testing
    semehr_dir = '/Users/daniyalarshad/EPCC/github/NationalSafeHaven'

    if os.path.isdir(os.path.expandvars("$HOME/SemEHR")):
        semehr_dir = os.path.expandvars("$HOME/SemEHR")
    if os.path.isdir('/opt/semehr'):
        semehr_dir = '/opt/semehr'
    if args.semehr_dir:
        semehr_dir = args.semehr_dir
    semehr_anon_cfg_file = os.path.join(semehr_dir, 'CogStack-SemEHR', 'anonymisation', 'conf', 'anonymisation_task.json') # i.e. /opt/semehr/CogStack-SemEHR/anonymisation/conf/anonymisation_task.json

    if os.path.isfile(args.input):
        anonymise_file(args.input, args.output, semehr_dir, semehr_anon_cfg_file, write_xml=args.write_xml)

    elif os.path.isdir(args.input):
        anonymise_dir(args.input, args.output, semehr_dir, semehr_anon_cfg_file, write_xml=args.write_xml)


if __name__ == '__main__':
    main()
