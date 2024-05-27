# ProcessDirectory

Primary Author: [Ally Hume](https://github.com/allyhume)

## Contents

1.  [Overview](#1-overview)
2.  [Setup / Installation](#2-setup--installation)
3.  [Exchange and Queue Settings](#3-exchange-and-queue-settings)
4.  [Config](#4-config)
5.  [Expectations](#5-expectations)
6.  [Class Diagram](#6-class-diagram)
7.  [Directory Scan Modes](#7-directory-scan-modes)

### 1. Overview

The ProcessDirectory app is a console app that runs and terminates rather than a microservice that runs forever. The app searches in and below a specified directory to find all directories that contain DICOM files. Each directory found which contains at least 1 dicom file will result in an `AccessionDirectoryMessage` being created. The behaviour of the scan can be changed with the `-f` option (see [#7](#7-directory-scan-modes)). When all the directories below the specified directory have been processed the program terminates.

### 2. Setup / Installation

-   Clone the project and build. Any NuGet dependencies should be automatically downloaded
-   Edit the yaml.default with the configuration for your environment
-   Run `ProcessDirctory.exe` from a commandline with the top level directory as the only argument.

### 3. Exchange and Queue Settings

| Read/Write | Type                      | Config setting                                              |
| ---------- | ------------------------- | ----------------------------------------------------------- |
| Write      | AccessionDirectoryMessage | `ProcessDirectoryOptions.AccessionDirectoryProducerOptions` |

### 4. Config

| YAML Section            | Purpose                                                                                                                                                                                                                                                 |
| ----------------------- | ------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| RabbitOptions           | Describes the location of the rabbit server for sending messages to                                                                                                                                                                                     |
| FileSystemOptions       | Describes the root location of all images this program will ever load, all directories provided in command line arguments must be subdirectories of this root in order that file names can be expressed as relative paths for downstream microservices. |
| ProcessDirectoryOptions | The exchange name that `AccessionDirectoryMessage` should be sent to when finding subdirectories with dicom files in them                                                                                                                               |

| Argument | Command Line Options    | Purpose                                                                          |
| -------- | ----------------------- | -------------------------------------------------------------------------------- |
| -d       | Directory root          | (Required) The directory of the image archive in which to begin folder discovery |
| -f       | Directory search format | (Optional) The directory scan mode to use. See []                                |

### 5. Expectations

Errors are [logged as normal for a MicroserviceHost](../../common/Smi.Common/README.md#logging)

### 6. Class Diagram

![Class Diagram](./Images/ClassDiagram.png)

### 7. Directory Scan Modes

Specified by the `-f` CLI argument. Options are:

-   [Default](Execution/DirectoryFinders/BasicDicomDirectoryFinder.cs). Performs a general recursive scan for files. The program does not look inside any subdirectories of directories which contain at least 1 file. This scan mode searches for DICOM files with the extension specified in the `FileSystemOptions.DicomSearchPattern` config option.

-   [PACS](Execution/DirectoryFinders/PacsDirectoryFinder.cs). Performs a scan which assumes files are located inside a particular directory structure. The `PACS` directory structure is of the form `<any root>/YYYY/MM/DD/ACC/<dicom>`. `ACC` represents accession directories. For each directory found which matches this pattern, an `AccessionDirectoryMessage` is produced. Note that this scan mode does not actually assert that there are any files inside the accession directories.

-   [List](Execution/DirectoryFinders/AccessionDirectoryLister.cs). Receives a file containing a list of accession directory paths. The accession directory path structure is expected to be of the form `<any root>/YYYY/MM/DD/ACC/`. For each path that matches this pattern, the existence of the directory and whether it contains DICOM files is checked. If the path meets all of the requirements, an `AccessionDirectoryMessage` is produced. Note that the input file path is passed in the same way as directory paths are passed to other operational modes.
