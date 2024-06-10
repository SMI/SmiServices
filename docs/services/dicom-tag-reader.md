# Dicom Tag Reader

Opens dicom files found in `AccessionDirectoryMessage` directories and converts to JSON as a `DicomFileMessage`. Also creates a summary record of the whole series as a `SeriesMessage`.

## Message Flow

| Read/Write | Message Type              | Config Property                               |
| ---------- | ------------------------- | --------------------------------------------- |
| Read       | AccessionDirectoryMessage | `DicomTagReaderOptions.QueueName`             |
| Write      | DicomFileMessage          | `DicomTagReaderOptions.ImageProducerOptions`  |
| Write      | SeriesMessage             | `DicomTagReaderOptions.SeriesProducerOptions` |

## YAML Configuration

| Key                     | Purpose                                                    |
| ----------------------- | ---------------------------------------------------------- |
| `DicomTagReaderOptions` | Main configuration for this service                        |
| `RabbitOptions`         | RabbitMQ connection options                                |
| `FileSystemOptions`     | Root directories where files will be discovered/anonymised |

## CLI Options

N/A.
