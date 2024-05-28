# DICOM Loader

The DicomLoader process reads a series of null-terminated filenames from STDIN, loading each one into MongoDB as it goes, terminating on ctrl-C or EOF.

## Example Usage

```console
find /data/dicom -type f -print0 | ./smi dicom-loader -y smi.yaml
```

DICOM files and archives containing DICOM files will be enumerated and loaded to MongoDB if not already present (checking by filename).

## YAML Configuration

MongoDB hostname and credentials are configured as per other SMI components, in particular the `MongoDatabases.DicomStoreOptions` section.
