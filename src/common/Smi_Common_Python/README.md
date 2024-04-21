# SMI Python library

Some useful functions in Python

## Requirements

```
deepmerge
pika
pydicom
pymongo
PyYAML
xml.etree (comes with python)
mysql-connector-python (which requires six, protobuf, dnspython) for IdentifierMapper
```

## Installation

Run `python3 ./setup.py bdist_wheel` to create `Smi_Services_Python-0.0.0-py3-none-any.whl`

Run `python3 ./setup.py install` to install (including dependencies) into your python site-packages
(whether that be global or inside a current virtualenv).

Note that the version number is read from AssemblyInfo.cs in a parent directory.

## Testing

Test all modules:

```
pytest SmiServices/*.py
```

Test each module individually, for example:

```
python3 -m pytest SmiServices/Dicom.py
python3 -m pytest SmiServices/DicomText.py
python3 -m pytest SmiServices/StructuredReport.py
```

## Usage

For example:

```
if 'SMI_ROOT' in os.environ:     # $SMI_ROOT/lib/python3
    sys.path.append(os.path.join(os.environ['SMI_ROOT'], 'lib', 'python3'))
from SmiServices import Mongo
from SmiServices import Rabbit
from SmiServices import Dicom
from SmiServices import DicomText
from SmiServices import StructuredReport as SR
from SmiServices import IdentifierMapper
```

## Dicom.py

Mostly low-level functions for reading DICOM files that are used by the DicomText module.

## DicomText.py

Provides a DicomText class which assists in parsing a DICOM Structured Report.
Also has functions for redacting the text given a set of annotations.
Uses the pydicom library internally.

Typical usage:

```
dicomtext = Dicom.DicomText(dcmname) # Reads the raw DICOM file
dicomtext.parse()                    # Analyses the text inside the ContentSequence
xmldictlist = Knowtator.annotation_xml_to_dict(xml.etree.ElementTree.parse(xmlfilename).getroot())
dicomtext.redact(xmldictlist)        # Redacts the parsed text using the annotations
dicomtext.write(redacted_dcmname)    # Writes out the redacted DICOM file
OR
write_redacted_text_into_dicom_file  # to rewrite a second file with redacted text
```

It also contains a `tag` method to return the value of the given named tag.

## IdentifierMapper.py

Provide a class CHItoEUPI for mapping from CHI to EUPI.
Create one instance with the SMI yaml dictionary to open a connection
to MySQL. Future instances can be created without the yaml and will
reuse the mysql connection.

```
IdentifierMapper.CHItoEUPI(yaml_dict)
eupi = IdentifierMapper.CHItoEUPI().lookup(chi)
```

## Knowtator.py

Provides a function for parsing the XML files containing annotations
as output by the SemEHR anonymiser and input to eHOST. The files are typically
named `.knowtator.xml` and have the format:

```
 <annotation>
  <mention id="anon.txt-1"/>
  <annotator id="semehr">semehr</annotator>
  <span end="44" start="34"/>
  <spannedText>16 year old</spannedText>
  <creationDate>Wed November 11 13:04:51 2020</creationDate>
 </annotation>
 <classMention id="anon.txt-1">
  <mentionClass id="semehr_sensitive_info">16 year old</mentionClass>
 </classMention>
```

The function `annotation_xml_to_dict` parses the XML and returns a suitable Python dict.

Also contains a function to write such XML files, useful when testing, or when converting from a Phi file.

## Mongo.py

Very simple wrapper around pymongo specifically for SMI.
Provides a `SmiPyMongoCollection` class. Typical usage:

```
mongodb = Mongo.SmiPyMongoCollection(mongo_host)
mongodb.setImageCollection('SR')
mongojson = mongodb.DicomFilePathToJSON('/path/file2')
print('MONGO: %s' % mongojson)
```

## Rabbit.py

Python interface to the SMI RabbitMQ messaging system.
Provides a class `smiMessage` which is inherited by task-specific classes
`CTP_Start_Message` and `IsIdentifiable_Start_Message`.
Provides classes `RabbitProducer` and `RabbitConsumer`.
Has sample functions `send_CTP_Start_Message` and `get_CTP_Output_Message` for testing.

One known problem with using RabbitMQ from Python is the lack of data types,
in particular no concept of a difference between 16-bit and 32-bit integers.
The `pika` library tries to be efficient by constructing a message using a 16-bit
integer if its value will fit, but the C# and Java program have been written to
explicitly expect a 32-bit integer, and they crash if given a 16-bit one.
The pika library currently has no way to request a 32-bit integer as the data
type is determined dynamically so we have to omit the Timestamp field from the
messages.

## StructuredReport.py

Provides a function `SR_parse` which can parse a Python dict containing a DICOM
Structured Report and return the content as a usable string. The dict can be
read from a DICOM file using pydicom or can be obtained from the MongoDB database
which represents the data in a similar but different format (i.e. no VR tag).

Some utility functions are used by the DicomText module.

# Parsing Structured Reports

Generally speaking you can work with Structured Reports in any of these ways:

-   dcm2json - outputs a JSON document (you can parse with `jq`, and use our
    `dicom_tag_string_replace.py` script to turn tag numbers into names)
-   read it with pydicom
-   read it from MongoDB

`dcm2json` - Depending on the source of the JSON, use `.val` or `.Value[0]`:

```
dcm2json file.dcm | dicom_tag_string_replace.py | jq dicom_tag_string_replace.py | \
 jq '..| select(.vr == "ST" or .vr == "PN" or .vr == "LO" or .vr == "UT" or .vr == "DA")? | .val'
```

`pydicom` - convert to a JSON dict

```
dicom_raw = pydicom.dcmread(filename)
dicom_raw_json = dicom_raw.to_json_dict()
```

`MongoDB` - using the SmiServices Mongo helper

```
mongodb = Mongo.SmiPyMongoCollection(mongo_host)
mongodb.setImageCollection('SR')
mongojson = mongodb.DicomFilePathToJSON(filepath)
```

To parse the actual SR document you can use either `DicomText` which reads
a DICOM file, and can parse the text, return the parsed text, and redact
the text given a list of redaction offsets, saving a new redacted DICOM file.

Alternatively the `StructuredReport` module can parse any of the various
flavours of JSON (from dcm2json, from pydicom, from mongodb).

## Method 1 - use the DicomText module

To read an original:

```
dicomtext = DicomText.DicomText(filename)
dicomtext.parse()
txt = dicomtext.text()
```

To redact, given an input_xml file:

```
xmlroot = xml.etree.ElementTree.parse(args.input_xml).getroot()
xmldictlist = Knowtator.annotation_xml_to_dict(xmlroot)
dicomtext.redact(xmldictlist)
dicomtext.write_redacted_text_into_dicom_file(args.output_dcm)
```

## Method 2 - use the StructuredReport module

From a JSON file (e.g. output by dcm2json):

```
with open('/lesion1-srdocument-medical.dcm.json') as fd:
  jdoc = json.load(fd)
sr = StructuredReport.StructuredReport()
sr.SR_parse(jdoc, 'doc', sys.stdout)
```

From a DICOM file:

```
ds = pydicom.dcmread('/lesion1-srdocument-medical.dcm')
sr = StructuredReport.StructuredReport()
sr.SR_parse(ds.to_json_dict(), 'doc_name', sys.stdout)
```

From MongoDB, get mongojson as above:

```
SR.SR_parse(mongojson, document_name, output_fd)
```

Agreed, the output to an open file may be inconvenient
so here's a temporary file tip:

```
with TemporaryFile(mode='w+', encoding='utf-8') as fd:
    SR.SR_parse(json_dict, document_name, fd)
    fd.seek(0)
    fd.read()
```

## Method 3 - use the pydicom walk method

```
def decode(filename):
    dicom_raw = pydicom.dcmread(filename)

    def dataset_callback(dataset, data_element):
    	if data_element.VR == 'SQ':
    		False
    	elif data_element.VR in ['SH', 'CS']:
    		False
    	elif data_element.VR == 'LO':
    		print('[[%s]]' % str(data_element.value))
    	else:
    		print('%s' % (str(data_element.value)))

    # Recurse only the values inside the ContentSequence
    for content_sequence_item in dicom_raw.ContentSequence:
    	content_sequence_item.walk(dataset_callback)
```

## Method 4 - use the pydicom recurse_tree method

```
def decode(filename):

    def recurse_tree(tree, dataset, parent):
    	for data_element in dataset:
    		# the node_id could be used as a unique reference
    		node_id = parent + "." + hex(id(data_element))
    		if isinstance(data_element.value, str):
    			# Useless data types are SH (eg. RE.05 or 99_OFFIS), CS (eg. CONTAINS)
    			if data_element.VR in ['SH', 'CS']:
    				False
    			elif data_element.VR == 'LO':
    				# LO is like a heading
    				print('[[%s]]' % (str(data_element.value)))
    			else:
    				# UT is text, DA is date, PN is name
    				print('%s' % (str(data_element.value)))
    		else:
    			# Non-string values are useless, sequences are handled below anyway
    			False #print('%s val = %s' % (node_id, str(data_element.value)))
    		if data_element.VR == "SQ":  # a sequence
    			for i, dataset in enumerate(data_element.value):
    				item_id = node_id + "." + str(i + 1)
    				sq_item_description = data_element.name.replace(" Sequence", "")  # XXX not i18n
    				item_text = "{0:s} {1:d}".format(sq_item_description, i + 1)
    				#print('%s seq = %s' % (item_id, item_text))
    				recurse_tree(tree, dataset, item_id)

    dicom_raw = pydicom.dcmread(filename)
    dicom_raw.decode()   # XXX should we decode to UTF?

    # Recurse only the values inside the ContentSequence
    # (to recurse the whole DICOM pass dicom_raw as second param).
    for content_sequence_item in dicom_raw.ContentSequence:
    	recurse_tree(None, content_sequence_item, '')
```
