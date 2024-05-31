# Dicom Reprocessor

Pulls documents from a MongoDB collection and republishes them to RabbitMQ as DicomFileMessages.

## Message Flow

| Read/Write | Message Type       | Config Property                                       |
| ---------- | ------------------ | ----------------------------------------------------- |
| Write      | `DicomFileMessage` | `DicomReprocessorOptions.ReprocessingProducerOptions` |

## YAML Configuration

| Key                                | Purpose                                                |
| ---------------------------------- | ------------------------------------------------------ |
| `DicomReprocessorOptions`          | Main configuration for this service                    |
| `RabbitOptions`                    | RabbitMQ connection options                            |
| `MongoDatabases.DicomStoreOptions` | Database connection info for the extraction data store |

## CLI Options

N/A.
