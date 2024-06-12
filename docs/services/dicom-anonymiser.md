# DICOM Anonymiser

_Note_: This service is in early development.

The DICOM Anonymiser service receives paths to DICOM files, anonymises them using the configured tool, and then produces the output anonymised filenames to another RabbitMQ queue.

## Message Flow

| Read/Write | Message Type               | Config Property                                           |
| ---------- | -------------------------- | --------------------------------------------------------- |
| Read       | `ExtractFileMessage`       | `DicomAnonymiserOptions.AnonFileConsumerOptions`          |
| Write      | `ExtractFileStatusMessage` | `DicomAnonymiserOptions.ExtractFileStatusProducerOptions` |

## YAML Configuration

| Key                      | Purpose                                                    |
| ------------------------ | ---------------------------------------------------------- |
| `DicomAnonymiserOptions` | Main configuration for this service                        |
| `RabbitOptions`          | RabbitMQ connection options                                |
| `FileSystemOptions`      | Root directories where files will be discovered/anonymised |

## CLI Options

N/A.
