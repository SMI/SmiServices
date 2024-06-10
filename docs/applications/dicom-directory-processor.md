# Dicom Directory Processor

This application searches recursively searches the specified directory to find all subdirectories which contain at least one DICOM file. For each such directory, an `AccessionDirectoryMessage` is sent.

Note that this application used to be called `ProcessDirectory`, and some config values still reference this.

## Message Flow

| Read/Write | Message Type                | Config Property                                             |
| ---------- | --------------------------- | ----------------------------------------------------------- |
| Write      | `AccessionDirectoryMessage` | `ProcessDirectoryOptions.AccessionDirectoryProducerOptions` |

## YAML Configuration

| Key                       | Purpose                             |
| ------------------------- | ----------------------------------- |
| `ProcessDirectoryOptions` | Main configuration for this service |
| `RabbitOptions`           | RabbitMQ connection options         |

## CLI Options

### Directory Scan Modes

The scan mode of the application can be changed with the `-f` option. Supported options are:

-   [default](/src/applications/Applications.DicomDirectoryProcessor/Execution/DirectoryFinders/BasicDicomDirectoryFinder.cs). Performs a general recursive scan for files. The program does not look inside any subdirectories of directories which contain at least 1 file. This scan mode searches for DICOM files with the extension specified in the `FileSystemOptions.DicomSearchPattern` config option.
-   [pacs](/src/applications/Applications.DicomDirectoryProcessor/Execution/DirectoryFinders/PacsDirectoryFinder.cs). Performs a scan which assumes files are located inside a particular directory structure. The `PACS` directory structure is of the form `<any root>/YYYY/MM/DD/ACC/<dicom>`. `ACC` represents accession directories. Note that this scan mode does not actually assert that there are any files inside the accession directories.
-   [list](/src/applications/Applications.DicomDirectoryProcessor/Execution/DirectoryFinders/AccessionDirectoryLister.cs). Receives a file containing a list of accession directory paths. The accession directory path structure is expected to be of the form `<any root>/YYYY/MM/DD/ACC/`. The existence of the directory and whether it contains DICOM files is checked.
-   [zips](/src/applications/Applications.DicomDirectoryProcessor/Execution/DirectoryFinders/ZipDicomDirectoryFinder.cs). Performs a scan for ZIP files (or DICOM files) in the specified directory.
