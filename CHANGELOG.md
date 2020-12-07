# Changelog

All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/), and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [Unreleased]

### Dependencies

- Bump System.IO.Abstractions.TestingHelpers from 13.2.2 to 13.2.4

## [1.13.0] - 2020-12-03

### Added

- Added new command line application TriggerUpdates for detecting and issuing UpdateValuesMessages (e.g. ECHI mapping changes)
- Added new service UpdateValues which propagates changes (e.g. ECHI mapping changes) throughout the deployed database tables.
- ConsensusRule for combining 2+ other rules e.g. SocketRules (See IsIdentifiable Readme.md for more details)
- Added runtime and total failures count to IsIdentifiable logs
- Added NoSuffixProjectPathResolver which generates anonymous image path names that do not contain "-an" (which is the default behaviour).
  -  To use, set `CohortExtractorOptions.ProjectPathResolverType` to `Microservices.CohortExtractor.Execution.ProjectPathResolvers.NoSuffixProjectPathResolver`
  -  For identifiable extractions, the NoSuffixProjectPathResolver is now used
-   Validation reports can now be created as either "Combined" (single report as before" or "Split" (a [pack](src/microservices/Microservices.CohortPackager/README.md) of reports including CSVs suitable for post-processing). This is configurable in the YAML config and can also be specified on the CLI when recreating reports for an extraction
-   Added JobCompletedAt to the validation reports
-   IsIdentifiable: Add support for ignoring OCR output less than `n` characters in length
-   IsIdentifiable: Add a test case for burned-in image text

### Changed

-   Update docs and make more keywords links to the relevant docs (#440)
-   Reduce memory usage on long-running microservices even when .Net assumes RAM is plentiful
-   Validation reports are now written to the project reports directory, instead of to a central reports directory

### Fixed

-   Fix mismatch in Java/C# messages for ExtractionModality
-   ExtractionFileCopier: Copy files relative to the extraction root not the global filesystem root
-   Fix implementation of minimum OCR length (before being reported) #471

### Dependencies

- Bump CsvHelper from 17.0.0 to 17.0.1
- Bump System.IO.Abstractions from 13.2.1 to 13.2.2
- Bump Moq from 4.15.1 to 4.15.2
- Bump System.IO.Abstractions.TestingHelpers from 13.2.1 to 13.2.2
- Bump CsvHelper from 16.2.0 to 17.0.0
- Bump JetBrains.Annotations from 2020.1.0 to 2020.3.0
- Bump jackson-dataformat-yaml from 2.11.3 to 2.12.0
- Bump jackson-databind from 2.11.3 to 2.12.0

## [1.12.2] - 2020-09-18

- Fix missing JSON fields from CTP output

## [1.12.1] - 2020-09-15

-   Remove reference to MongoDB.Driver in Smi.Common.MongoDb.csproj since it caused a version conflict in the output packages

## [1.12.0] - 2020-09-14

### Added

-   [breaking] Add identifiable extraction support
    -   New service "FileCopier" which sits in place of CTP for identifiable extractions and copies source files to their output dirs
    -   Changes to MongoDB extraction schema, but backwards compatibility has been tested
    -   RabbitMQ extraction config has been refactored. Queues and service config files need to be updated
-   Add [SecurityCodeScan](https://security-code-scan.github.io/) tool to build chain for .NET code
-   Add "no filters" extraction support. If specified when running ExtractorCLI, no file rejection filters will be applied by CohortExtractor. True by default for identifiable extractions
-   Added caching of values looked up in NLP/rulesbase for IsIdentifiable tool
-   Added new rejector that throws out values (e.g. patient IDs) whose IDs are stored in a database table.  Set `RejectColumnInfos` option in yaml to enable this
-   Added a check to QueryToExecuteResult for RejectReason being null when Reject is true.

### Changed

-   [breaking] Environment variables are no longer required.  Previous settings now appear in configuration file
    - Environment variable `SMI_LOGS_ROOT` is now `GlobalOptions.LogsRoot`
    - Environment variable `MONGO_SERVICE_PASSWORD` is now `MongoDbOptions.Password`
    - Removed `ISIDENTIFIABLE_NUMTHREADS` as it didn't work correctly anyway
-   Extraction report: Group PixelData separately and sort by length
-   IsIdentifiable Reviewer 'Symbols' rule factory now supports digits only or characters only mode (e.g. use `\d` for digits but leave characters verbatim)
-   IsIdentifiable Reviewer 'symbols' option when building Regex now builds capture groups and matches only the failing parts of the input string not the full ProblemValue.  For example `MR Head 12-11-20` would return `(\d\d-\d\d-\d\d)$`

### Fixed

-   Fix the extraction output directory to be `<projId>/extractions/<extractname>`

### Dependencies

-   Bump fo-dicom.Drawing from 4.0.5 to 4.0.6
-   Bump fo-dicom.NetCore from 4.0.5 to 4.0.6
-   Bump HIC.BadMedicine.Dicom from 0.0.6 to 0.0.7
-   Bump HIC.DicomTypeTranslation from 2.3.0 to 2.3.1
-   Bump HIC.FAnsiSql from 1.0.2 to 1.0.5
-   Bump HIC.RDMP.Dicom from 2.1.6 to 2.1.10
-   Bump HIC.RDMP.Plugin from 4.1.6 to 4.1.8
-   Bump HIC.RDMP.Plugin.Test from 4.1.6 to 4.1.8
-   Bump Microsoft.CodeAnalysis.CSharp.Scripting from 3.6.0 to 3.7.0
-   Bump Microsoft.Extensions.Caching.Memory from 3.1.6 to 3.1.8
-   Bump Microsoft.NET.Test.Sdk from 16.6.1 to 16.7.1
-   Bump MongoDB.Driver from 2.11.0 to 2.11.2
-   Bump System.IO.Abstractions from 12.1.1 to 12.1.9
-   Bump System.IO.Abstractions.TestingHelpers from 12.1.1 to 12.1.9
-   Bump Terminal.Gui from 0.81.0 to 0.89.4

## [1.11.1] - 2020-08-12

-   Set PublishTrimmed to false to fix bug with missing assemblies in prod.

## [1.11.0] - 2020-08-06

### Added

- DicomDirectoryProcessor and TagReader support for zip archives
  - Expressed in notation `/mydrive/myfolder/myzip.zip!somesubdir/my.dcm`
  - Requires command line `-f zips`

### Changed

-   Improved the extraction report by summarising verification failures
-   Start MongoDB in replication mode in the Travis builds
-   Switch to self-contained .Net binaries to avoid dependency on host runtime package
-   NationalPACSAccessionNumber is now allowed to be null in all messages

### Dependencies

-   Bump HIC.RDMP.Plugin from 4.1.5 to 4.1.6
-   Bump MongoDB.Driver from 2.10.4 to 2.11.0
-   Bump System.IO.Abstractions from 12.0.10 to 12.1.1
-   Bump System.IO.Abstractions.TestingHelpers from 12.0.10 to 12.1.1
-   Bump jackson-dataformat-yaml from 2.11.1 to 2.11.2

## [1.10.0] - 2020-07-31

### Changed

- Updated the extraction report to be more human-readable #320, #328
- Add CLI option to CohortPackager to allow an existing report to be recreated #321
- Added a runsettings file for NUnit to allow configuration of test output. Fixes an issue with TravisCI and NUnit3TestAdapter v3.17.0, which caused the test output to spill to over 20k lines.

### Dependencies

- Bump HIC.FAnsiSql from 0.11.1 to 1.0.2
- Bump HIC.RDMP.Dicom from 2.1.5 to 2.1.6
- Bump HIC.RDMP.Plugin from 4.1.3 to 4.1.5
- Bump Magick.NET-Q16-AnyCPU from 7.20.0 to 7.21.1
- Bump Microsoft.Extensions.Caching.Memory from 3.1.5 to 3.1.6
- Bump System.IO.Abstractions from 12.0.1 to 12.0.10
- Bump System.IO.Abstractions from 12.0.1 to 12.0.2
- Bump System.IO.Abstractions.TestingHelpers from 12.0.1 to 12.0.2
- Bump com.fasterxml.jackson.dataformat.jackson-dataformat-yaml from 2.11.0 to 2.11.1
- Bump org.mockito.mockito-core from 3.3.3 to 3.4.6

## [1.9.0] - 2020-06-22

### Added

- Added image extraction blacklist rejector.
  - Configure with `Blacklists` option (specify a list of Catalogue IDs)
  - Catalogues listed must include one or more column(s) StudyInstanceUID, SeriesInstanceUID, SOPInstanceUID.
  - Records in the referenced table will blacklist where any UID is found (StudyInstanceUID, SeriesInstanceUID or SOPInstanceUID).  This allows blacklisting an entire study or only specific images.
  - [breaking] Config on live system may need updated
- Change the extraction directory generation to be `<projname>/image-requests/<extractname>`. Fixes [MVP Service #159](https://dev.azure.com/smiops/MVP%20Service/_workitems/edit/159/)

### Fixed

- Fixed IsIdentifiable rule order being the order the files are detected in rules directory (Now goes IgnoreRules=>ReportRules=>SocketRules)
- Adjust log handling in CTP anonymiser to use SMIlogging setup
- IsIdentifiable case-sensitive rules now implemented with property 
- Bufix for fo-dicom image handling race condition in Release mode builds (issue #238)

### Changed

- Refactored `WhiteListRule` to inherit from `IsIdentifiableRule` (affects serialization).  
  - Parent property `As` replaces `IfClassification`
  - `CaseSensitive` replaces `IfPatternCaseSensitive` and `IfPartPatternCaseSensitive` (Also fixes serialization bug)
- Bump CommandLineParser from 2.7.82 to 2.8.0
- Bump CsvHelper from 15.0.4 to 15.0.5
- Bump HIC.BadMedicine.Dicom from 0.0.5 to 0.0.6
- Bump HIC.DicomTypeTranslation from 2.2.2 to 2.3.0
- Bump HIC.RDMP.Dicom from 2.0.9 to 2.1.5
- Bump HIC.RDMP.Plugin from 4.0.2 to 4.1.3
- Bump Magick.NET-Q16-AnyCPU from 7.16.0 to 7.20.0
- Bump Microsoft.CodeAnalysis.CSharp.Scripting from 3.5.0 to 3.6.0
- Bump Microsoft.Extensions.Caching.Memory from 3.1.3 to 3.1.5
- Bump MongoDB.Driver from 2.10.3 to 2.10.4
- Bump StackExchange.Redis from 2.1.30 to 2.1.58
- Bump System.IO.Abstractions from 10.0.8 to 12.0.1
- Bump YamlDotNet from 8.1.0 to 8.1.2
- Bump fo-dicom.Drawing from 4.0.4 to 4.0.5
- Pinned fo-dicom.NetCore to 4.0.5

## [1.8.1] - 2020-04-17

### Fixed

- Fix null check bug in CohortPackager when no files match the extraction filter

## [1.8.0] - 2020-04-16

### Added

- Added Terminal.Gui at version 0.81.0
- Added data/IsIdentifiableRules

### Changed

- \[Breaking\] Promote the PT modality to its own collection in MongoDB
- \[Breaking\] Renamed `RedisHost` to `RedisConnectionString` in the config options for clarity
- Update to .Net Core 3.1 (supported until Dec 2022) since 2.2 support ended last year
- Switch CohortExtractor to use batched message producers
- Simplify the Travis build script
- Fail any integration tests in CI if a required service is not available (instead of skipping)
- Specified LangVersion 8.0 in all project files
- Upgraded CommandLineParser from 2.5.0 to 2.7.82
- Upgraded CsvHelper from 12.1.2 to 15.0.4
- Upgraded HIC.Rdmp.Dicom from 2.0.8 to 2.0.9
- Upgraded JetBrains.Annotations from 2019.1.3 to 2020.1.0
- Upgraded Magick.NET-Q16-AnyCPU from 7.15.1 to 7.16.0
- Upgraded Microsoft.CodeAnalysis.CSharp.Scripting from 3.5.0-beta2-final to 3.5.0
- Upgraded MongoDB.Driver from 2.9.3 to 2.10.3
- Upgraded StackExchange.Redis from 2.0.601 to 2.1.30
- Upgraded System.Drawing.Common from 4.6.0 to 4.7.0
- Upgraded System.IO.Abstractions from 7.0.7 to 10.0.8
- Upgraded YamlDotNet from 6.0.0 to 8.1.0

### Fixed

- Fixed logging to directories in the Java services

## [1.7.0] - 2020-03-30

### Added

- Added undo feature to IsIdentifiableReviewer
- Java microservices now log to SMI_LOGS_ROOT

### Changed

- Upgraded HIC.DicomTypeTranslation from `2.1.2` to `2.2.0`
  - This includes an upgrade to fo-dicom from `4.0.1` to `4.0.4`
- Upgraded fo-dicom.Drawing from `4.0.1` to `4.0.4`
- Upgraded HIC.RdmpDicom from `2.0.7` to `2.0.8`

## [1.6.0] - 2020-03-17

### Changed

- Update CohortPackager for new extraction design
  - Consume messages from CTP (failed anonymisation) and IsIdentifiable (verification)
  - Add support for extraction by modality
  - Remove the final check for the anonymised file. IsIdentifiable handles this already
  - Refactor tests

- Start to refactor core RabbitMqAdapter code to allow unit testing

## [1.5.2] - 2020-03-12

### Added
 
 - IsIdentifiableReviewer considers rule capture groups when performing redactions (e.g. can now handle custom rules like `^(Ninewells)`)
 - IsIdentifiableReviewer adds comment with time/user to rules file e.g. `#TZNind - 3/10/2020 1:17:17 PM`
 - IsIdentifiableReviewer checks custom patterns match the original Failure
 - IsIdentifiable microservice was started with --service but can now be started with the service verb allowing it to take additional options. It should now be started with `service -y file.yaml`
 - IsIdentifiable no longer reads Rules.yaml from the current directory. It now has a command line option --RulesDirectory, to go with the already existing --RulesFile. That will read all \*.yaml files in the given directory. However when run as a microservice the yaml file specifies a DataDirectory; the RulesDirectory will implicitly be a subdirectory called IsIdentifiableRules from which all \*.yaml files will be read.

### Changed
  - IsIdentifiableReviewer now tries to isolate 'Problem Words' when generating it's suggested Updater Regex rules (e.g. now suggests `^Ninewells` instead of `^Ninewells\ Spike\ CT$`.)
  
## [1.5.1] - 2020-03-06

- Improved usability of IsIdentifiableReviewer

## [1.5.0] - 2020-03-05

- \[Breaking\] Updated RabbitMQ extraction config to match extraction plan v2
- Refactor Java exception handling and use of threads
- `TessDirectory` option in [IsIdentifiable] now expects tesseract models file to exist (no longer downloads it on demand)
- Addeed support for outsourcing classification (e.g. NLP) to other processes via TCP (entered in [SocketRules] in `Rules.yaml`)
- IsIdentifiable NLP text classification now outsourced via TCP to any services configured in 
  - [StanfordNER implementation written in java](./src/microservices/uk.ac.dundee.hic.nerd/README.md)
- New CohortExtractor yaml config option `ProjectPathResolverType` which determines the folder structure for extracted images
- Added [script](./utils/rabbitmq-config-tester/rabbitmq-config-tester.py) to verify RabbitMQ config files
- Added `DynamicRejector` which takes its cohort extraction rules from a script file (of CSharp code)
- Added new application for reviewing IsIdentifiable output files

### Fixed

- Corrected the GetHashCode implementation in the MessageHeader class

## [1.4.5] - 2020-02-26

- Add clean shutdown hook for IdentifierMapper to clean up the worker threads

## [1.4.4] - 2020-02-25

- Update Travis config and Java library install shell script to resolve some Travis stability issues
- Adjust batching so workers queue replies/acks while a worker thread commits those asynchronously, allowing elastic batch sizes (qosprefetch setting now controls maximum batch size, parallelism capped at 50)

## [1.4.3] - 2020-02-21

### Changed

- Batch up RabbitMQ messages/acks in IdentifierMapper to avoid contention with the message publishing persistence

## [1.4.2] - 2020-02-18

### Added

- Added unit test for AccessionDirectoryLister as part of DicomDirectoryProcessor tests

### Changed

- Make performance counters in RedisSwapper atomic for thread-safety
- Clean up threads when using threaded mode in RabbitMQAdapter
- Use explicit threads rather than Task queueing in IdentifierMapper

## [1.4.1] - 2020-02-17

### Added

- Added randomisation in the retry delay on DicomRelationalMapper (and set minimum wait duration to 10s)

### Fixed

- Fixed DLE Payload state being wrong when retrying batches (when it is half / completely consumed)
- Added lock on producer sending messages in IdentifierMapper

## [1.4.0] - 2020-02-14

### Added

- Added in memory caching of the last 1024 values when using Redis wrapper for an IdentifierSwapper
- Added some parallelism and marshalling of backend queries to improve throughput in IdentifierSwapper
- Added temporary flag for RabbitMQAdapter parallelism for the above. Only enabled for the IdentifierMapper for now
- Added new mode to DicomDirectoryProcessor which allows reading in a list of accession directories

## [1.3.1] - 2020-02-13

### Changed

- Pinned fo-dicom to v4.0.1

## [1.3.0] - 2020-02-06

### Added

- Added (optional) DicomFileSize property to ETL pipeline.  Add to template(s) with:
```yaml
  - ColumnName: DicomFileSize
    AllowNulls: true
    Type:
      CSharpType: System.Int64
```

- Added new microservice IsIdentifiable which scans for personally identifiable information (in databases and dicom files)
- Added support for custom rules in IsIdentifiable (entered in `Rules.yaml`)
  - Rules are applied in the order they appear in this file
  - Rules are applied before any other classifiers (i.e. to allow whitelisting rules)
- Added `RedisSwapper` which caches answers from any other swapper.  Set `RedisHost` option in yaml to use.

### Changed

- Updated RDMP and Dicom plugins
- Refactor Java exception handling and use of threads

## [1.2.3] - 2020-01-09

### Changed

- RabbitMQAdapter: Improve handling of timeouts on connection startup

### Added

- Improved logging in IdentifierSwappers

### Changed

- Guid swapper no longer limits input identifiers to a maximum of 10 characters

### Fixed

- Fixed DicomRelationalMapper not cleaning up STAGING table remnants from previously failed loads (leading to crash)

## [1.2.2] - 2020-01-08

### Fixed

- RAW to STAGING migration now lists columns explicitly (previously used `SELECT *` which could cause problems if RAW and STAGING column orders somehow differed)

## [1.2.1] - 2020-01-06

### Added

- Added the `set-sleep-time-ms` control message to DicomReprocessor

### Changed

- Updated Rdmp.Dicom nuget package to 2.0.6

## [1.2.0] - 2019-12-12

### Added

- Improved travis deployment
- (Re-)added Smi.NLog.config in builds
- Added better CLI argument descriptions for DicomReprocessor
- Added error logging for RabbitMQ bad Ack responses 
  - Previously: `BasicReturn for TEST.IdentifiableImageExchange`
  - Now : `BasicReturn for Exchange 'TEST.IdentifiableImageExchange' Routing Key 'reprocessed' ReplyCode '312' (NO_ROUTE)`
- Added new swapper `TableLookupWithGuidFallbackSwapper` which performs lookup substitutions but allocates guids for lookup misses
- Added Travis CI build & deploy for all services

### Changed

- Make exceptions on startup clearer
- Updated to latest RDMP API (4.0.1)
- `TableLookupSwapper` now throws consistent error if the provided table does not exist during `Setup` (previously it would error with DBMS specific error message at lookup time)

### Fixed

- Fixed freeze condition when exchanges are not mapped to queues
- IdentifierMapper now loads all FAnsi database implementations up front on startup

## [1.1.0] - 2019-11-22

### Added

- Improvements to unit and integration tests
- Documentation fixes
- Config file for Dependabot
- Test for DicomFile SkipLargeTags option. Closes [#19](https://dev.azure.com/SmiOps/MVP%20Service/_workitems/edit/19)

### Changed

### C\# dependencies
- Bumped HIC.DicomTypeTranslation from 1.0.0.3 to 2.1.2
- Bumped HIC.RDMP.Plugin from 3.1.1 to 4.0.1-rc2
- Bumped Newtonsoft.Json from 12.0.2 to 12.0.3
- Bumped RabbitMQ.Client from 5.1.0 to 5.1.2
- Bumped System.IO.Abstractions from 4.2.17 to 7.0.7
- Bumped MongoDB.Driver from 2.8.0 to 2.9.3

### Java dependencies

- Bumped jackson-databind from 2.9.6 to 2.9.10.0

## [1.0.0] - 2019-11-18

First stable release after importing the repository from the private [SMIPlugin](https://github.com/HicServices/SMIPlugin) repo.

### Added

- ForGuidIdentifierSwapper automatically creates it's mapping database if it does not exist on the server referenced (previously only table was automatically created)

### Changed

- Updated to [Rdmp.Dicom 2.0.2](https://github.com/HicServices/RdmpDicom/blob/master/CHANGELOG.md#202-2019-11-13)
- Updated to [Rdmp.Core 3.2.1](https://github.com/HicServices/RDMP/blob/develop/CHANGELOG.md#321---2019-10-30)

### Removed

- Anonymous `MappingTableName` must now be fully specified to pass validation (e.g. `mydb.mytbl`). Previously skipping database portion was supported.


[Unreleased]: https://github.com/SMI/SmiServices/compare/v1.13.0...develop
[1.13.0]:  https://github.com/SMI/SmiServices/compare/v1.12.2...v1.13.0
[1.12.2]:  https://github.com/SMI/SmiServices/compare/v1.12.1...v1.12.2
[1.12.1]:  https://github.com/SMI/SmiServices/compare/v1.12.0...v1.12.1
[1.12.0]:  https://github.com/SMI/SmiServices/compare/v1.11.1...v1.12.0
[1.11.1]:  https://github.com/SMI/SmiServices/compare/v1.11.0...v1.11.1
[1.11.0]:  https://github.com/SMI/SmiServices/compare/v1.10.0...v1.11.0
[1.10.0]:  https://github.com/SMI/SmiServices/compare/v1.9.0...v1.10.0
[1.9.0]:  https://github.com/SMI/SmiServices/compare/v1.8.1...v1.9.0
[1.8.1]:  https://github.com/SMI/SmiServices/compare/v1.8.0...v1.8.1
[1.8.0]:  https://github.com/SMI/SmiServices/compare/v1.7.0...v1.8.0
[1.7.0]:  https://github.com/SMI/SmiServices/compare/v1.6.0...v1.7.0
[1.6.0]:  https://github.com/SMI/SmiServices/compare/v1.5.2...v1.6.0
[1.5.2]:  https://github.com/SMI/SmiServices/compare/v1.5.1...v1.5.2
[1.5.1]:  https://github.com/SMI/SmiServices/compare/v1.5.0...v1.5.1
[1.5.0]:  https://github.com/SMI/SmiServices/compare/v1.4.5...v1.5.0
[1.4.5]:  https://github.com/SMI/SmiServices/compare/v1.4.4...v1.4.5
[1.4.4]:  https://github.com/SMI/SmiServices/compare/v1.4.3...v1.4.4
[1.4.3]:  https://github.com/SMI/SmiServices/compare/v1.4.2...v1.4.3
[1.4.2]:  https://github.com/SMI/SmiServices/compare/v1.4.1...v1.4.2
[1.4.1]:  https://github.com/SMI/SmiServices/compare/v1.4.0...v1.4.1
[1.4.0]:  https://github.com/SMI/SmiServices/compare/v1.3.1...v1.4.0
[1.3.1]:  https://github.com/SMI/SmiServices/compare/v1.3.0...v1.3.1
[1.3.0]:  https://github.com/SMI/SmiServices/compare/v1.2.3...v1.3.0
[1.2.3]:  https://github.com/SMI/SmiServices/compare/v1.2.2...v1.2.3
[1.2.2]:  https://github.com/SMI/SmiServices/compare/v1.2.1...v1.2.2
[1.2.1]:  https://github.com/SMI/SmiServices/compare/1.2.0...v1.2.1
[1.2.0]:  https://github.com/SMI/SmiServices/compare/1.1.0...1.2.0
[1.1.0]: https://github.com/SMI/SmiServices/compare/1.0.0...1.1.0
[1.0.0]: https://github.com/SMI/SmiServices/releases/tag/1.0.0

[IsIdentifiable]: ./src/microservices/Microservices.IsIdentifiable/README.md
[SocketRules]: ./src/microservices/Microservices.IsIdentifiable/README.md#socket-rules
