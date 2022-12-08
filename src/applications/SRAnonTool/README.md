# SRAnonTool

The SRAnonTool is a set of programs to assist with anonymising DICOM Structured Reports.

## Requirements

Python package requirements:

-   pydicom
-   pymongo
-   deepmerge

Python library requirements:

-   The SmiServices python library, see `src/common/Smi_Common_Python/`

External tool requirements:

-   The SmiServices CTP anonymiser
-   SemEHR/CogStack anonymiser (or the test stub)
-   dcm2json (for testing; optional; from the dcmtk package)
-   jq (for testing; optional)
-   diff (for testing)

## Installation

The scripts require dependencies which need to be found via the `PATH` or `PYTHONPATH`.

Copy the scripts to `$SMI_ROOT/scripts`

Install the python library `SmiServices` to `$SMI_ROOT/lib/python3/` or a virtualenv

Ensure the python package dependencies are installed system-wide or in a virtualenv on the host machine.

Modify the `default.yaml` file: in the section `CTPAnonymiserOptions` add `SRAnonTool: /path/to/CTP_SRAnonTool.sh`

Ensure the `default.yaml` file contains the necessary `FileSystemOptions`, `LoggingOptions>LogsRoot`, `MongoDatabases`, `RabbitOptions`, etc.

Install the SemEHR/CogStack anonymiser, which currently uses the following directories (which may be symbolic links):

-   `/opt/semehr/CogStack` - contains the scripts
-   `/opt/semehr/data/input_docs` - raw text copied here will be anonymised
-   `/opt/semehr/data/anonymised` - output anonymous text and xml files
-   `/opt/gcp/bio-yodie-1-2-1` - the UMLS dictionary
-   `/opt/gcp/gcp-2.5-18658` - java libraries

The old SemEHR anonymiser requires Python2; all the other scripts require Python3, including the new SemEHR anonymiser.
If using the test stub then only the data directories are required and Python2 is not required.

## Usage as part of CTP

Configure CTP to call the script CTP_SRAnonTool.sh when it detects a DICOM file with `SR` in the `Modality` tag, by editing `default.yaml` as above. CTP will call the script with two options:

-   `-i input.dcm` - the raw DICOM file before anonymisation
-   `-o output.dcm` - the DICOM file which CTP has already anonymised

The script will extract the text from the `input.dcm` file, anonymise it, and write the redacted text into the `output.dcm` file, which must already exist.

## Standalone usage

The script `CTP_SRAnonTool.sh` calls three components:

-   `CTP_DicomToText.py` - extracts the text from the raw DICOM file into a format suitable for SemEHR-CogStack.
-   `CogStack-SemEHR/anonymisation/anonymiser.py` - this is the script in SemEHR-CogStack which anonymises the text.
-   `CTP_XMLToDicom.py` - redacts the text from the raw DICOM file and write the redacted text into the output DICOM file.

Usage: `[-e virtualenv] [-s semehr_dir]  -i read_from.dcm  -o write_into.dcm`

The `-e` option can be used to activate a virtual environment by sourcing the `bin/activate` script.
The default is to set `PYTHONPATH` to `$SMI_ROOT/lib/python3`

The SemEHR directory (`/opt/semehr`) can be changed with the `-s` option for testing (it's not set when called by CTP).

### `CTP_DicomToText.py`

This program can be used as part of the SRAnonTool pipeline or it can be used standalone to extract documents in bulk for later SemEHR processing.

Usage: `-y default.yaml -i input.dcm -o output [-m metadata_output] [--semehr-unique]`

`-y default.yaml` - may be specified more than once if the configuration parameters are spread across multiple yaml files.

`-i input.dcm` - full path to the input DICOM file, or a partial path to be extracted from MongoDB, or a StudyDate to extract all records that day from MongoDB.

`-o output` - full path to the output text file, or directory for multiple files.

`-m metadata_output` - full path to the output metadata json file, or directory for multiple files.

`--semehr-unique` - if extracting a StudyDate from MongoDB then ignore any documents which have a SOPInstanceUID that is already in the SemEHR MongoDB database. This is intended to allow reprocessing of any documents that previously failed without having to reprocess the whole day.

If metadata output is requested then JSON output files are created containing the values of these tags:
`SOPClassUID, SOPInstanceUID, StudyInstanceUID, SeriesInstanceUID, ContentDate, ModalitiesInStudy, PatientID`.
The latter is mapped from CHI to EUPI.

The MongoDB configuration read from the yaml files needs to be in `MongoDatabases | DicomStoreOptions` and `SemEHRStoreOptions`.
The former is to read DICOM documents from the `dicom.image_SR` database.collection;
the latter is to check if the SOPInstanceUID is already in the `semehr.semehr_results` database.collection.

The MySQL configuration read from the yaml files needs to be in `IdentifierMapperOptions`
with keys `MappingConnectionString, MappingTableName, SwapColumnName, ReplacementColumnName`.
This is used to map PatientID.

Examples:

```
* CTP_DicomToText.py -i /path/to/file.dcm -o output.txt
* CTP_DicomToText.py -i 2015/01/01/AccNum/file.dcm -o output.txt -y smi_dataLoad.yaml
* CTP_DicomToText.py -i 20150101 -o output_dir -m metadata_dir -y smi_dataLoad.yaml
```

### `clinical_doc_wrapper.py`

This script performs the anonymisation.
It is the old Python-2 version and is no longer used.

Usage: `[-s semehr_dir] [-i input_docs] [-o anonymised]` in the stub version

It must be called with the current directory being the location of the script.

It reads all the files in the `/data/input_docs` directory. For each input file it write a slightly modified file with the same name into the `/data/anonymised` directory, basically the text with some header metadata removed, plus it writes a file of the same name plus `.knowtator.xml` appended containing annotations in XML format.

The SemEHR version requires Python2; the test stub requires Python3. Note that this script is no longer used in the new SemEHR anonymiser.

The test stub of this program has no requirement on current directory. It is best suited when tested with the given test DICOM file as it only fakes the anonymisation of the word `Baker`. The `-s` option can be used to specify the SemEHR directory instead of `/opt/semehr` which is useful when testing; this option is not present in the original.

## `anonymiser.py`

This script performs the anonymisation.

Usage: `./anonymiser.py /path/to/anonymisation_task.json`

It must be called with the current directory being the location of the script.

The template configuration file is typically in the `anonymisation/conf` directory
but should have the following elements modified for each run of the anonymiser:

```
.text_data_path=${semehr_input_dir}
.anonymisation_output=${semehr_output_dir}
.extracted_phi=${semehr_output_dir}/phi
.grouped_phi_output=${semehr_output_dir}/phi_grouped
.logging_file=${semehr_output_dir}/log
.annotation_mode=false # temporary false until the knowtator XML output is fixed
```

### `CTP_XMLToDicom.py`

Usage: `-y default.yaml -i input.dcm -x input.xml -o output.dcm`

`-y default.yaml` - may be specified more than once if the configuration parameters are spread across multiple yaml files

`-i input.dcm` - full path to the raw DICOM file

`-x input.xml` - full path to the XML file containing annotations

`-o output.dcm` - full path to the anonymised DICOM file, which must already exist, where the redacted text is written

## Testing

In the test subdirectory, run

```
./CTP_SRAnonTool_test.py [-s semehr_dir] [-d file.dcm] [-p pattern_to_redact] [-y default.yaml]
```

That will read `report10html.dcm` and run the above scripts, checking that the output matches what is expected.
It will print `SUCCESS` and exit 0 if successful, exit 1 if the output is not as expected.

The defaults are:

`-s semehr_dir` - `/opt/semehr`

`-d file.dcm` - `report10html.dcm` (a public sample file manually edited to include HTML fragments)

`-p pattern_to_redact` - `Baker` (to suit the example DICOM file)

`-y default.yaml` - `../../../../data/microserviceConfigs/default.yaml`

To run in the test directory

```
mkdir -p ./data/input_docs
mkdir -p ./data/anonymised
./CTP_SRAnonTool_test.py -s .
```
