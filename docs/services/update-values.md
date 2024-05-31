# Update Values

This service services `UpdateValuesMessage` which is a request to update a concept e.g. PatientID in one or more tables. Each message describes a single query (although multiple values can be updated at once).

## Message Flow

| Read/Write | Message Type          | Config Property       |
| ---------- | --------------------- | --------------------- |
| Read       | `UpdateValuesMessage` | `UpdateValuesOptions` |

## YAML Configuration

| Key                   | Purpose                                                                                                                                      |
| --------------------- | -------------------------------------------------------------------------------------------------------------------------------------------- |
| `UpdateValuesOptions` | Main configuration for this service.                                                                                                         |
| `RabbitOptions`       | RabbitMQ connection options                                                                                                                  |
| `RDMPOptions`         | RDMP platform database connection options, which keeps track of load configurations, available datasets to extract images from (tables) etc. |

## CLI Options

N/A.

## Usage Notes

Each message processed will result in one or more UPDATE SQL statements being run. These may run on different servers and different DBMS (Oracle, MySql, Sql Server, Postgres). This ensures a relatively consistent 'whole system' update of a given fact.
