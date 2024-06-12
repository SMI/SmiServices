# Dicom Relational Mapper

Runs an RDMP data load configuration (`LoadMetadata`) with a batch of `DicomFileMessage` to load Dicom Tag data into a relational database (MySql or Microsoft Sql Server). It is designed to work on many images at once for performance. The load configuration is configurable through the main RDMP client.

## Message Flow

| Read/Write | Message Type       | Config Property                |
| ---------- | ------------------ | ------------------------------ |
| Read       | `DicomFileMessage` | `DicomRelationalMapperOptions` |

## YAML Configuration

| Key                            | Purpose                                                                                                                                                                                                                                                                                                                                                                                                  |
| ------------------------------ | -------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| `DicomRelationalMapperOptions` | The queue from which to read DicomFileMessage and the ID of the `LoadMetadata` load configuration. A load configuration is a sequence of steps to modify/clean data such that it is loadable into the final live tables. The LoadMetadata is designed to be modified through the RMDP user interface and is persisted in the LoadMetadata table (and other related tables) of the RDMP platform database |

| `RabbitOptions` | RabbitMQ connection options |
| `RDMPOptions` | RDMP platform database connection options, which keeps track of load configurations, available datasets to extract images from (tables) etc. |

## CLI Options

N/A.

## Usage Notes

### Adding Tags

Relational database tables have an initial schema out of the box based on an image template.

Once the system has gone live, data analysts will still be able to add new tags to existing database tables through the RDMP user interface using tag promotion.

### Audit

In addition to logging to NLog like other microservices, the data load itself will be audited in the RDMP relational logging database. This includes facts such as how many records were loaded (UPDATES / INSERTS) and any problems encountered.

In order to improve traceability the 'image' table of every set of imaging database tables (Study + Series + Image) has a field `messageguid` which contains the Guid of the input message to DicomRelationalMapper that resulted in the record being part of the batch. This guid can be traced back through the file logs to see all microservices that acted to result in that record being in the final live database.

Finally all tables (Study, Series and Image) have a validFrom and a dataLoadRunId field which record which load batch they were last part of and when it was executed.

In the event that a load fails (e.g. due to primary key collisions) the RAW / STAGING databases that contain data being worked on in the load are left intact for debugging. In such a case all messages in the batch are Nacked.
