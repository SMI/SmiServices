# Identifier Mapper

This service takes serialized Dicom files as `DicomFileMessage` messages and uses an `ISwapIdentifiers` to replace the top level `DicomTag.PatientID` tag for an anonymous representation. If there is no PatientID then the message is nacked. If the `ISwapIdentifiers` returns null then the message is nacked with the reason provided by `string GetSubstitutionFor(string toSwap, out string reason)`.

## Message Flow

| Read/Write | Message Type     | Config Property                                     |
| ---------- | ---------------- | --------------------------------------------------- |
| Read       | DicomFileMessage | `IdentifierMapperOptions`                           |
| Write      | DicomFileMessage | `IdentifierMapperOptions.AnonImagesProducerOptions` |

## YAML Configuration

| Key                       | Purpose                              |
| ------------------------- | ------------------------------------ |
| `IdentifierMapperOptions` | Main configuration for this service. |
| `RabbitOptions`           | RabbitMQ connection options          |

In the `IdentifierMapperOptions` key:

- Pick an implementation of `ISwapIdentifiers` e.g. `Microservices.IdentifierMapper.Execution.IdentifierSwapper` and enter the full Type name into default.yaml `IdentifierMapperOptions.SwapperType`.
- Specify the mapping table database details\*
  - MappingConnectionString, the connection string to use to connect to the mapping server
  - MappingDatabaseType, either MicrosoftSQLServer or MYSQLServer
  - MappingTableName, the table on the mapping server that contains the identifier mapping table\* (identifiable=>anonymous)
  - SwapColumnName, the column in the `MappingTableName` that will contain the expected (identifiable) input values to replace.
  - ReplacementColumnName, the column in the `MappingTableName` that contains the replacement values.
- Decide if you want to [use a Redis](#redis) cache.

\*The table/connection string details are at the disposal of the `ISwapIdentifiers` chosen. Some might ignore them completely or might manually create the mapping table themselves (e.g. `ForGuidIdentifierSwapper`)

### Redis

If you are using an `ISwapper` implementation that consults a large mapping database e.g. 10 million then you may benefit from using a Redis caching database. Install Redis and set the `RedisHost` option in the config file e.g.

```yaml
IdentifierMapperOptions:
    SwapperType: "Microservices.IdentifierMapper.Execution.Swappers.TableLookupSwapper"
    RedisHost: localhost
```

All lookup results will be cached in the Redis server (both successful lookups and misses).

If you update your lookup table you will have to manually flush the Redis server (if desired).

## CLI Options

N/A.
