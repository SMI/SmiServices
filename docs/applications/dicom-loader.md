# DICOM Loader

The DicomLoader process reads a series of null-terminated filenames from STDIN, loading each one into MongoDB and/or the relational database as it goes, terminating on ctrl-C or EOF.

## Message Flow

N/A.

## YAML Configuration

| Key                                | Purpose                    |
| ---------------------------------- | -------------------------- |
| `MongoDatabases.DicomStoreOptions` | MongoDB connection options |

## CLI Options

In addition to any common options:

```console
  -d, --delete                Optional. Delete existing database entries rather than skipping matching files.
  -p, --parallelism=n         Optional. Number of worker threads to run in parallel.
  -m, --match=string          Optional. If set, perform this MongoDB query and load matching data to SQL.
  -r, --ramLimit=n            Optional. How many GiB of RAM to use before flushing the load queue, default 16.
  -s, --sql                   Optional. If set, load data to the SQL database not just Mongo.
```

## Example Usage

```console
find /data/dicom -type f -print0 | ./smi dicom-loader -y smi.yaml -s
```

DICOM files and archives containing DICOM files will be enumerated and loaded to both MongoDB and SQL if not already present (checking by filename).
