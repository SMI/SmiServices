
# Changelog

All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/), and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [Unreleased]

## Removed

- Anonymous `MappingTableName` must now be fully specified to pass validation (e.g. `mydb.mytbl`).  Previously skipping database portion was supported.

## Changed

- Updated to [Rdmp.Dicom 2.0.2](https://github.com/HicServices/RdmpDicom/blob/master/CHANGELOG.md#202-2019-11-13)
- Updated to [Rdmp.Core 3.2.1](https://github.com/HicServices/RDMP/blob/develop/CHANGELOG.md#321---2019-10-30)

### Added

- ForGuidIdentifierSwapper automatically creates it's mapping database if it does not exist on the server referenced (previously only table was automatically created)

[Unreleased]: https://github.com/SMI/SmiServices/compare/develop
