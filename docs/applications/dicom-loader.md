# DICOM Loader

The DicomLoader process reads a series of null-terminated filenames from STDIN, loading each one into MongoDB as it goes, terminating on ctrl-C or EOF.

## Message Flow

N/A.

## YAML Configuration

| Key                                | Purpose                    |
| ---------------------------------- | -------------------------- |
| `MongoDatabases.DicomStoreOptions` | MongoDB connection options |

## CLI Options

## Example Usage

```console
find /data/dicom -type f -print0 | ./smi dicom-loader -y smi.yaml
```

DICOM files and archives containing DICOM files will be enumerated and loaded to MongoDB if not already present (checking by filename).
