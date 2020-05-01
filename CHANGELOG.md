# Changelog

All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/), and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [Unreleased]

### Added

- Added image extraction blacklist rejector.
  - Configure with `Blacklists` option (specify a list of Catalogue IDs)
  - Catalogues listed must include one or more column(s) StudyInstanceUID, SeriesInstanceUID, SOPInstanceUID.
  - Records in the referenced table will blacklist where any UID is found (StudyInstanceUID, SeriesInstanceUID or SOPInstanceUID).  This allows blacklisting an entire study or only specific images.

### Fixed

- Adjust log handling in CTP anonymiser to use SMIlogging setup
- IsIdentifiable case-sensitive rules now implemented with property 

### Changed

- Refactored `WhiteListRule` to inherit from `IsIdentifiableRule` (affects serialization).  
  - Parent property `As` replaces `IfClassification`
  - `CaseSensitive` replaces `IfPatternCaseSensitive` and `IfPartPatternCaseSensitive` (Also fixes serialization bug)

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


[Unreleased]: https://github.com/SMI/SmiServices/compare/v1.8.1...develop
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
