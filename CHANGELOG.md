# Changelog

All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/), and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [Unreleased]

### Changed

- \[Breaking\] Updated RabbitMQ extraction config to match extraction plan v2
- Refactor Java exception handling and use of threads
- `TessDirectory` option in [IsIdentifiable] now expects tesseract models file to exist (no longer downloads it on demand)

### Added

- Added new microservice [IsIdentifiable] which scans for personally identifiable information (in databases and dicom files)
- IsIdentifiable runs standalone or as a service in the extraction pipeline (where it validates anonymised files)
- Added support for custom rules in [IsIdentifiable] (entered in `Rules.yaml`)
  - Rules are applied in the order they appear in this file
  - Rules are applied before any other classifiers (i.e. to allow whitelisting rules)
- Addeed support for outsourcing classification (e.g. NLP) to other processes via TCP (entered in [SocketRules] in `Rules.yaml`)
- IsIdentifiable NLP text classification now outsourced via TCP to any services configured in 
  - [StanfordNER implementation written in java](./src/microservices/uk.ac.dundee.hic.nerd/README.md)
- New CohortExtractor yaml config option `ProjectPathResolverType` which determines the folder structure for extracted images
- Added [script](./utils/rabbitmq-config-tester/rabbitmq-config-tester.py) to verify RabbitMQ config files


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

### Changed

- Make exceptions on startup clearer

## [1.2.0-rc1] - 2019-12-10

### Changed

- Updated to latest RDMP API (4.0.1)
- `TableLookupSwapper` now throws consistent error if the provided table does not exist during `Setup` (previously it would error with DBMS specific error message at lookup time)

### Fixed

- Fixed freeze condition when exchanges are not mapped to queues
- IdentifierMapper now loads all FAnsi database implementations up front on startup

### Added

- Added better CLI argument descriptions for DicomReprocessor
- Added error logging for RabbitMQ bad Ack responses
  - Previously: `BasicReturn for TEST.IdentifiableImageExchange`
  - Now : `BasicReturn for Exchange 'TEST.IdentifiableImageExchange' Routing Key 'reprocessed' ReplyCode '312' (NO_ROUTE)`
- Added new swapper `TableLookupWithGuidFallbackSwapper` which performs lookup substitutions but allocates guids for lookup misses
- Added Travis CI build & deploy for all services

## [1.1.0] - 2019-11-22

## Added

- Improvements to unit and integration tests
- Documentation fixes
- Config file for Dependabot
- Test for DicomFile SkipLargeTags option. Closes [#19](https://dev.azure.com/SmiOps/MVP%20Service/_workitems/edit/19)

## Changed

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

## Changed

- Updated to [Rdmp.Dicom 2.0.2](https://github.com/HicServices/RdmpDicom/blob/master/CHANGELOG.md#202-2019-11-13)
- Updated to [Rdmp.Core 3.2.1](https://github.com/HicServices/RDMP/blob/develop/CHANGELOG.md#321---2019-10-30)

## Removed

- Anonymous `MappingTableName` must now be fully specified to pass validation (e.g. `mydb.mytbl`). Previously skipping database portion was supported.

[unreleased]: https://github.com/SMI/SmiServices/compare/v1.2.3...develop
[1.2.3]: https://github.com/SMI/SmiServices/compare/v1.2.2...v1.2.3
[1.2.2]: https://github.com/SMI/SmiServices/compare/v1.2.1...v1.2.2
[1.2.1]: https://github.com/SMI/SmiServices/compare/1.2.0...v1.2.1
[1.2.0]: https://github.com/SMI/SmiServices/compare/1.1.0-rc1...1.2.0
[1.2.0-rc1]: https://github.com/SMI/SmiServices/compare/1.1.0...1.2.0-rc1
[1.1.0]: https://github.com/SMI/SmiServices/compare/1.0.0...1.1.0
[1.0.0]: https://github.com/SMI/SmiServices/releases/tag/1.0.0

[IsIdentifiable]: ./src/microservices/Microservices.IsIdentifiable/README.md
[SocketRules]: ./src/microservices/Microservices.IsIdentifiable/README.md#socket-rules