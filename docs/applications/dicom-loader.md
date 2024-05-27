# ProcessDirectory

Primary Author: [James Sutherland](https://github.com/jas88)

## Contents

1.  [Overview](#1-overview)
2.  [Setup / Installation](#2-setup--installation)

### 1. Overview

The DicomLoader process reads a series of null-terminated filenames from STDIN, loading each one to Mongo as it goes, terminating on ctrl-C or EOF.

### 2. Setup / Installation

`find /data/dicom -type f -print0 | ./smi dicom-loader -y smi.yaml`

DICOM files and archives containing DICOM files will be enumerated and loaded to Mongo if not already present (checking by filename).

MongoDB hostname and credentials are configured as per other SMI components, in particular the MongoDatabases section (where DicomStoreOptions has HostName, Port, UserName, Password and DatabaseName values).
