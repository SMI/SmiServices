# CTP Anonymiser

The CTP Anonymiser receives paths to DICOM files , anonymises them using the CTP DICOM [Anonymizer](https://mircwiki.rsna.org/index.php?title=The_CTP_DICOM_Anonymizer), and then produces the output anonymised filenames to another RabbitMQ queue. The choice of tags to anonymise is determined by a provided anonymisation script file.

### Structured Report anonymisation

The procedure for anonymising Structured Reports (the SR modality) is to have
CTP proceed as normal, removing almost all tags which may contain PII, then
extract the text from the original DICOM file and pass it through an external
anonymisation process, and then insert the redacted text into CTP's output file,
thus reconstructing the text content part of the DICOM with anonymous text.

To do this requires an external tool to be configured as `SRAnonTool` in the yaml,
which should be the full path to the tool, unless it is in the current directory.
The tool is called with `-i input.dcm -o output.dcm`, where input.dcm is the raw
original DICOM file and output.dcm is the output from CTP. The tool is only called
for DICOM files which have the `Modality` tag equal to `SR`.

Any failures in the execution of the tool will result in a complete failure of
the CTP anonymisation process for this file. Even though CTP may have successfully
removed all PII from the file, it will not proceed if it cannot insert redacted text.

## Message Flow

| Read/Write | Message Type               | Config Property                                         |
| ---------- | -------------------------- | ------------------------------------------------------- |
| Read       | `ExtractFileMessage`       | `CTPAnonymiserOptions.AnonFileConsumerOptions`          |
| Write      | `ExtractFileStatusMessage` | `CTPAnonymiserOptions.ExtractFileStatusProducerOptions` |

## YAML Configuration

| Key                    | Purpose                                                    |
| ---------------------- | ---------------------------------------------------------- |
| `CTPAnonymiserOptions` | Main configuration for this service                        |
| `RabbitOptions`        | RabbitMQ connection options                                |
| `FileSystemOptions`    | Root directories where files will be discovered/anonymised |

## CLI Options

The anonymisation script can be specified using the `-a` option. An example script can be viewed [here](/data/ctp/ctp-allowlist.script).
