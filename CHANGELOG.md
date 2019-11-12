
# Changelog

All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/), and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [Unreleased]

## Changed

- When no NLog config file is specified in Microservice arguments, the MicroserviceHost will now attempt to load `Smi.NLog.config` or `SmiTest.NLog.config` or failing this any single file ending `.NLog.config` (multiple / zero matches still result in error)
- Updated to [Rdmp.Dicom 1.3.2](https://github.com/HicServices/RdmpDicom/blob/master/CHANGELOG.md#132-2019-10-30)
- Updated to [Rdmp.Core 3.2.1](https://github.com/HicServices/RDMP/blob/develop/CHANGELOG.md#321---2019-10-30)

### Added

- RabbitMqAdapter now outputs HostName, VirtualHost, UserName and Port when failing to validate settings.

[Unreleased]: https://github.com/SMI/SmiServices/compare/develop
