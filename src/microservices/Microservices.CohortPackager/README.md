# Cohort Packager

Primary Author: [Ruairidh MacLeod](https://github.com/rkm)

## Contents

1.  [Overview](#1-overview)
2.  [Setup / Installation](#2-setup--installation)
3.  [Queue Settings](#3-queue-settings)
4.  [Config](#4-config)
5.  [Expectations](#5-expectations)
6.  [Reports](#6-reports)

### 1. Overview

Collects all information regarding an extraction job, and monitors the filesystem for the anonymised files. Persists all information to a MongoDB collection.

Produces validation reports for each extraction suitable for review by research coordinators before the extraction files are released. See [reports section](#6-reports). Reports are created automatically when an extraction is detected as being complete, and can also be manually recreated on the CLI by passing the `-r` or `--recreate-reports` flag with the corresponding extraction GUID.

### 2. Setup / Installation

-   Clone the project and build. Any NuGet dependencies should be automatically downloaded
-   Setup a yaml file with the configuration for your environment
-   Run `CohortPackager.exe` with your yaml config

### 3. Exchange and Queue Settings

| Read/Write | Type                      | Config setting                                      |
| ---------- | ------------------------- | --------------------------------------------------- |
| Read       | ExtractRequestMessage     | `DicomReprocessorOptions.ExtractRequestInfoOptions` |
| Read       | ExtractRequestInfoMessage | `DicomReprocessorOptions.ExtractFilesInfoOptions`   |
| Read       | ExtractFileStatusMessage  | `DicomReprocessorOptions.AnonImageStatusOptions`    |

### 4. Config

| YAML Section       | Purpose                                                               |
| ------------------ | --------------------------------------------------------------------- |
| JobWatcherTickrate | How often the filesystem is checked for anonymised files (in seconds) |

### 5. Expectations

Errors are [logged as normal for a MicroserviceHost](../../common/Smi.Common/README.md#logging)

### 6. Reports

When an extraction is completed, report(s) are created detailing any errors or validation failures relating to the set of files that have been produced. There are currently 2 formats:

-   `Combined`. A single file useful for smaller extractions which can be manually reviewed. Format:

    ```markdown
    # SMI extraction validation report for testProj1/extract1

    Job info:

    -   Job submitted at: 2020-11-19T17:48:59
    -   Job completed at: 2020-11-19T17:49:07
    -   Job extraction id: 62c1b363-8181-435c-9eca-11e1f095043f
    -   Extraction tag: SeriesInstanceUID
    -   Extraction modality: Unspecified
    -   Requested identifier count: 2
    -   Identifiable extraction: No
    -   Filtered extraction: Yes

    Report contents:

    -   Verification failures
        -   Summary
        -   Full Details
    -   Blocked files
    -   Anonymisation failures

    ## Verification failures

    ### Summary

    -   Tag: ScanOptions (2 total occurrence(s))
        -   Value: 'FOO' (2 occurrence(s))

    ### Full details

    -   Tag: ScanOptions (1 total occurrence(s))
        -   Value: 'FOO' (1 occurrence(s))
            -   series-2-anon-2.dcm
            -   series-2-anon-3.dcm

    ## Blocked files

    -   ID: series-1
        -   1x 'rejected - blah'

    ## Anonymisation failures

    -   file 'series-2-anon-1.dcm': 'Couldn't anonymise'

    --- end of report ---
    ```

-   `Split`. A pack (directory) of files which can be used for larger extractions to script the review process by reading them into external analysis tools. 6 files are currently produced for each extraction:

    -   `README.md`. Summary information for the extract

        ```markdown
        # SMI extraction validation report for testProj1/extract1

        Job info:

        -   Job submitted at: 2020-11-19T17:50:41
        -   Job completed at: 2020-11-19T17:50:48
        -   Job extraction id: 08f22a41-2a3d-4fa3-b9d2-380aabc5a59b
        -   Extraction tag: SeriesInstanceUID
        -   Extraction modality: Unspecified
        -   Requested identifier count: 2
        -   Identifiable extraction: No
        -   Filtered extraction: Yes

        Files included:

        -   README.md (this file)
        -   pixel_data_summary.csv
        -   pixel_data_full.csv
        -   pixel_data_word_length_frequencies.csv
        -   tag_data_summary.csv
        -   tag_data_full.csv

        This file contents:

        -   Blocked files
        -   Anonymisation failures

        ## Blocked files

        -   ID: series-1
            -   1x 'rejected - blah'

        ## Anonymisation failures

        -   file 'series-2-anon-1.dcm': 'Couldn't anonymise'

        --- end of report ---
        ```

    -   `pixel_data_full.csv`. Full details for all potential PII contained in the DICOM pixel data from the post-anonymisation OCR scan

        ```csv
        TagName,FailureValue, FilePath
        PixelData,Mr. Foobar,path/to/file.dcm
        ```

    -   `pixel_data_summary.csv`. Summary details for all potential PII contained in the DICOM pixel data from the post-anonymisation OCR scan

        ```csv
        TagName,FailureValue,Occurrences,RelativeFrequencyInTag,RelativeFrequencyInReport
        PixelData,Mr. Foobar,1,1,1
        ```

    -   `pixel_data_word_frequencies.csv`. Frequency analysis of the word lengths contained in the DICOM pixel data from the post-anonymisation OCR scan

        ```csv
        WordLength,Count,RelativeFrequencyInReport
        10,1,1
        ```

    -   `tag_data_full.csv`. Full details for all potential PII contained in the DICOM tag data from the post-anonymisation NER scan

        ```csv
        TagName,FailureValue,FilePath
        ScanOptions,FOO,series-2-anon-2.dcm
        ```

    -   `tag_data_summary.csv`. Summary details for all potential PII contained in the DICOM tag data from the post-anonymisation NER scan
        ```csv
        TagName,FailureValue,Occurrences,RelativeFrequencyInTag,RelativeFrequencyInReport
        ScanOptions,FOO,1,1,1
        ```

Note that for identifiable extractions (where no anonymisation is applied), only the combined report is supported. This will have the format:

```markdown
# SMI extraction validation report for testProj1/extract1

Job info:

-   Job submitted at: 2020-11-19T17:57:12
-   Job completed at: 2020-11-19T17:57:20
-   Job extraction id: 3f400e06-19cb-45ae-9545-3a5310f426f3
-   Extraction tag: StudyInstanceUID
-   Extraction modality: MR
-   Requested identifier count: 1
-   Identifiable extraction: Yes
-   Filtered extraction: Yes

Report contents:

-   Missing file list (files which were selected from an input ID but could not be found)

## Missing file list

-   study-1-orig-2.dcm

--- end of report ---
```
