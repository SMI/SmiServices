# IsIdentifiable

This service evaluates 'data' for personally identifiable values (e.g. names). It can source data from a veriety of places (e.g. databases, file system).

This service relies on the [IsIdentifiable](https://github.com/SMI/IsIdentifiable) library for identifying personal data. See the [docs on setting up rules](https://github.com/SMI/IsIdentifiable/blob/main/IsIdentifiable/README.md).

## Message Flow

| Read/Write | Message Type                 | Config Property                                       |
| ---------- | ---------------------------- | ----------------------------------------------------- |
| Read       | `ExtractedFileStatusMessage` | `IsIdentifiableOptions`                               |
| Write      | `IsIdentifiableMessage`      | `IsIdentifiableOptions.IsIdentifiableProducerOptions` |

## YAML Configuration

| Key                     | Purpose                              |
| ----------------------- | ------------------------------------ |
| `IsIdentifiableOptions` | Main configuration for this service. |
| `RabbitOptions`         | RabbitMQ connection options          |

## CLI Options

N/A.
