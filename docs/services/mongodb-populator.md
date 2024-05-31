# MongoDB Populator

Stores DICOM tag data from `DicomFileMessage`s and/or `SeriesMessage`s in a MongoDB database.

## Message Flow

| Read/Write | Message Type       | Config Property                                                |
| ---------- | ------------------ | -------------------------------------------------------------- |
| Read       | `DicomFileMessage` | `MongoDbPopulatorOptions.ImageQueueConsumerOptions.QueueName`  |
| Read       | `SeriesMessage`    | `MongoDbPopulatorOptions.SeriesQueueConsumerOptions.QueueName` |

## YAML Configuration

| Key                                | Purpose                                                                 |
| ---------------------------------- | ----------------------------------------------------------------------- |
| `MongoDbPopulatorOptions`          | Main configuration for this service                                     |
| `RabbitOptions`                    | RabbitMQ connection options                                             |
| `MongoDatabases.DicomStoreOptions` | Contains the connection strings and database names to write tag data to |

## CLI Options

N/A.
