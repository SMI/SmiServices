# Trigger Updates

TODO

This application issues update messages designed for consumption by the [UpdateValues](../services/update-values.md) service.

using the options specified in the `TriggerUpdatesOptions` config setting.

### 3. Exchange and Queue Settings

| Read/Write | Type                  | Config setting          |
| ---------- | --------------------- | ----------------------- |
| Write      | `UpdateValuesMessage` | `TriggerUpdatesOptions` |

### 4. Config

| YAML Section            | Purpose                                                                                     |
| ----------------------- | ------------------------------------------------------------------------------------------- |
| RabbitOptions           | Describes the location of the rabbit server for sending messages to                         |
| IdentifierMapperOptions | Describes the location of the mapping table when using the `mapper` verb with this command. |
| TriggerUpdatesOptions   | The exchange name that `UpdateValuesMessage` should be sent to when detecting updates       |

Arguments vary by verb, use `./TriggerUpdates mapper --help` to see required arguments.

### 5. Expectations

Errors are [logged as normal for a MicroserviceHost](../../common/Smi.Common/README.md#logging)

#### 5.1 Aliases

When using mapping updates it is possible for certain corner case sequences to result in crossed mappings, especially when aliases are permitted and where those aliases change over time

Initial Lookup Table

| Private | Release |
| ------- | ------- |
| A       | 111     |
| B       | 222     |

_Any live database values for A or B will have only the Release identifiers (111 and 222)_

Update lookup table with fact A=B

| Private | Release |
| ------- | ------- |
| A       | 111     |
| B       | 111     |

_Triggering an update at this point will merge 111 and 222 in the live database_

Once an alias has been established the lookup cannot successfully be updated to reverse the alias e.g. reverting it back to the initial state.

### 6. Class Diagram
