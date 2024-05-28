# Trigger Updates

This application checks for updates to identifier mapping tables, and sends messages designed for consumption by the [UpdateValues](../services/update-values.md) service.

It issues updates to different database types specified by the CLI verb. Supported verbs are currently:

```console
  mapper     Triggers updates based on new identifier mapping table updates
```

## Mapper

In addition to any common CLI options, the following arguments are supported:

```console
  -d, --DateOfLastUpdate    Required. The last known date where live tables and mapping table were in sync.  Updates will be issued for records changed after this date
  -f, --FieldName           The field name of the release identifier in your databases e.g. PatientID.  Only needed if different from the mapping table swap column name e.g. ECHI
  -q, --Qualifier           Qualifier for values e.g. '.  This should be the DBMS qualifier needed for strings/dates.  If patient identifiers are numerical then do not specify this option
```

### Mapper Alias Note

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

## YAML Configuration

Uses the `TriggerUpdatesOptions` config key to determine the publishing options.
