# File Copier

This service receives paths to DICOM files, copies each to the specified output path, and then produces the output anonymised filenames to another RabbitMQ queue.

## Message Flow

| Read/Write | Message Type               | Config Property                               |
| ---------- | -------------------------- | --------------------------------------------- |
| Read       | `ExtractFileMessage`       | `FileCopierOptions`                           |
| Write      | `ExtractFileStatusMessage` | `FileCopierOptions.CopyStatusProducerOptions` |

## YAML Configuration

| Key                 | Purpose                                                    |
| ------------------- | ---------------------------------------------------------- |
| `FileCopierOptions` | Main configuration for this service                        |
| `RabbitOptions`     | RabbitMQ connection options                                |
| `FileSystemOptions` | Root directories where files will be discovered/anonymised |

## CLI Options

N/A.
