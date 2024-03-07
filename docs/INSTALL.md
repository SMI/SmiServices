# Full smiServices set-up script

## Prerequisites

-   Linux system with sufficient disk, CPU and RAM capacity for your needs
-   MySQL 8 or later
-   MongoDB

## Assumptions

This document and script assumes the file layout below – substitute according to your own environment/choices:

Everything underneath /imaging:

-   /imaging/bin – storage for SMI and RDMP tools
-   /imaging/conf – configuration files for SMI and RDMP
-   /imaging/db – mount point for faster (SSD/RAID10) storage for databases
-   /imaging/db/sql
-   /imaging/db/mongo
-   /imaging/data – main bulk storage area for DICOM files

MySQL databases will be:

-   smi
-   smi_isolation

## Limitations

-   These instructions currently deliver only the load/indexing stage, not an extraction pipeline.

## Installation steps

-   Download the files in this directory (currently [install.sh](install.sh) and [schema.sql](schema.sql)), make install.sh executable.

```
chmod +x install.sh
./install.sh
```

TODO:

-   Set up ExternalDatabaseServer for smi_isolation
-   GuidDatabaseNamer for RAW/STAGING?

Copy the file 'default.yaml' from the SMI distribution to /imaging/conf/smi.yaml and make the following changes:

-   LogsRoot: '/imaging/logs'
-   FileSystemRoot: '/imaging'
-   ExtractRoot: '/imaging'
-   YamlDir: '/imaging/conf/rdmp'
-   LoadMetadataId: (ID from: 'basename /imaging/conf/rdmp/LoadMetadata/\*.yaml')
