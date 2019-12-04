
# Changelog

All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/), and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [Unreleased]

### Changed

- Updated to latest RDMP API (4.0.1)

### Fixed

- Fixed freeze condition when exchanges are not mapped to queues

### Added

- Added better CLI argument descriptions for DicomReprocessor
- Added error logging for RabbitMQ bad Ack responses 
  - Previously: `BasicReturn for TEST.IdentifiableImageExchange`
  - Now : `BasicReturn for Exchange 'TEST.IdentifiableImageExchange' Routing Key 'reprocessed' ReplyCode '312' (NO_ROUTE)`

## [1.1.0] - 2019-11-22

## Added

- Improvements to unit and integration tests
- Documentation fixes
- Config file for Dependabot
- Test for DicomFile SkipLargeTags option. Closes [#19](https://dev.azure.com/SmiOps/MVP%20Service/_workitems/edit/19)

## Changed

### C#

- Bumped HIC.DicomTypeTranslation from 1.0.0.3 to 2.1.2
- Bumped HIC.RDMP.Plugin from 3.1.1 to 4.0.1-rc2
- Bumped Newtonsoft.Json from 12.0.2 to 12.0.3
- Bumped RabbitMQ.Client from 5.1.0 to 5.1.2
- Bumped System.IO.Abstractions from 4.2.17 to 7.0.7
- Bumped MongoDB.Driver from 2.8.0 to 2.9.3

### Java

- Bumped jackson-databind from 2.9.6 to 2.9.10.0


## [1.0.0] - 2019-11-18

First stable release after importing the repository from the private [SMIPlugin](https://github.com/HicServices/SMIPlugin) repo.

### Added

- ForGuidIdentifierSwapper automatically creates it's mapping database if it does not exist on the server referenced (previously only table was automatically created)

## Changed

- Updated to [Rdmp.Dicom 2.0.2](https://github.com/HicServices/RdmpDicom/blob/master/CHANGELOG.md#202-2019-11-13)
- Updated to [Rdmp.Core 3.2.1](https://github.com/HicServices/RDMP/blob/develop/CHANGELOG.md#321---2019-10-30)

## Removed

- Anonymous `MappingTableName` must now be fully specified to pass validation (e.g. `mydb.mytbl`).  Previously skipping database portion was supported.


[Unreleased]: https://github.com/SMI/SmiServices/compare/1.1.0...develop
[1.1.0]: https://github.com/SMI/SmiServices/compare/1.0.0...1.1.0
[1.0.0]: https://github.com/SMI/SmiServices/releases/tag/1.0.0
