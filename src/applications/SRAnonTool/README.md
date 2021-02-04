# SRAnonTool

The SRAnonTool is a set of programs to assist with anonymising DICOM Structured Reports.

## Requirements

Python package requirements:
* pydicom
* pymongo
* deepmerge

Python library requirements:
* The SmiServices python library, see `src/common/Smi_Common_Python/`

External tool requirements:
* The SmiServices CTP anonymiser
* SemEHR/CogStack anonymiser

## Installation

The scripts require dependencies which need to be found via the `PATH` or `PYTHONPATH`.

Copy the scripts to `$SMI_ROOT/scripts`

Copy the python library `Smi_Common_Python` to `$SMI_ROOT/lib/python3/`

Ensure the python package dependencies are installed system-wide on the host machine.

Modify the `default.yaml` file: in the section `CTPAnonymiserOptions` add `SRAnonTool: /path/to/SRAnonTool.sh`

Ensure the `default.yaml` file contains the necessary `FileSystemOptions`, `LogsRoot`, `MongoDatabases`, `RabbitOptions`, etc.

Install the SemEHR/CogStack anonymiser, which currently uses the following directories (which may be symbolic links):

* `/opt/semehr/CogStack` - contains the scripts
* `/data/input_docs` - raw text copied here will be anonymised
* `/data/anonymised` - output anonymous text and xml files
* `/opt/gcp/bio-yodie-1-2-1` - the UMLS dictionary
* `/opt/gcp/gcp-2.5-18658` - java libraries

## Usage as part of CTP

Configure CTP to call the script SRAnonTool.sh when it detects a DICOM file with `SR` in the `Modality` tag, by editing `default.yaml` as above. CTP will call the script with two options:
* `-i input.dcm` - the raw DICOM file before anonymisation
* `-o output.dcm` - the DICOM file which CTP has already anonymised

The script will extract the text from the `input.dcm` file, anonymise it, and write the redacted text into the `output.dcm` file, which must already exist.

## Standalone usage

The script `SRAnonTool.sh` calls three components:

* `CTP_DicomToText.py` - extracts the text from the raw DICOM file into a format suitable for SemEHR-CogStack.
* `clinical_doc_wrapper.py` - this is the component within SemEHR-CogStack which anonymises the text.
* `CTP_XMLToDicom.py` - redacts the text from the raw DICOM file and write the redacted text into the output DICOM file.

### `CTP_DicomToText.py`

Usage: `-y default.yaml -i input.dcm -o outfile`

`-y default.yaml` - may be specified more than once if the configuration parameters are spread across multiple yaml files

`-i input.dcm` - full path to the input DICOM file

`-o output` - full path to the output text file, or directory

### `clinical_doc_wrapper.py`

Usage: no command line arguments

It must be called with the current directory being the location of the script.

It reads all the files in the `/data/input_docs` directory. For each input file it write a slightly modified file with the same name into the `/data/anonymised` directory, basically the text with some header metadata removed, plus it writes a file of the same name plus `.knowtator.xml` appended containing annotations in XML format.

### `CTP_XMLToDicom.py`

Usage: `-y default.yaml -i input.dcm -x input.xml -o output.dcm`

`-y default.yaml` - may be specified more than once if the configuration parameters are spread across multiple yaml files

`-i input.dcm` - full path to the raw DICOM file

`-x input.xml` - full path to the XML file containing annotations

`-o output.dcm` - full path to the anonymised DICOM file, which must already exist, where the redacted text is written


## Testing

In the test subdirectory, run

```
./CTP_SRAnonTool_test.py
```

That will read `report10html.dcm` and run the above scripts, checking that the output matches what is expected.
It will print `SUCCESS` if successful.
