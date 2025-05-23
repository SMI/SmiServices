# Changelog

All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/),
and this project adheres to
[Semantic Versioning](https://semver.org/spec/v2.0.0.html).

Changes since the previous release can be found in the [news](./news) directory.
A raw git diff can be seen [here][unreleased].

<!--next-->

## [6.1.0] 2025-04-19

### Feature

- [#2140](https://github.com/SMI/SmiServices/pull/2140) by rkm. Enable postgres in CI tests

### Fix

- [#2141](https://github.com/SMI/SmiServices/pull/2141) by rkm. Fix identifier-mapper to work with postgres. Adds `MappingTableSchema` to `IdentifierMapperOptions`.

## [6.0.0] 2025-01-08

### Feature

- [#2018](https://github.com/SMI/SmiServices/pull/2018) by rkm. Bump .NET SDK to 9.0.100
- [#2024](https://github.com/SMI/SmiServices/pull/2024) by rkm. Target net9.0 / C# 13.0
- [#2041](https://github.com/SMI/SmiServices/pull/2041) by rkm. Support restricting which extraction keys are allowed
- [#2042](https://github.com/SMI/SmiServices/pull/2042) by rkm. Add options for pooled extractions (not used yet)
- [#2053](https://github.com/SMI/SmiServices/pull/2053) by rkm. Finish dicom-anonymiser CTP implementation and expand tests
- [#2068](https://github.com/SMI/SmiServices/pull/2068) by rkm. Refactor message Ack/Nack using events
- [#2069](https://github.com/SMI/SmiServices/pull/2069) by rkm. Delete IModel from Consumer and update usage in tests
- [#2072](https://github.com/SMI/SmiServices/pull/2072) by rkm. Refactor message deserialization from RabbitMQ

### Bugfix

- [#2012](https://github.com/SMI/SmiServices/pull/2012) by rkm. Add dependency on System.Private.Uri to resolve CVE
- [#2013](https://github.com/SMI/SmiServices/pull/2013) by rkm. Require RID in dotnet clean script to workaround lack of `--use-current-runtime`
- [#2025](https://github.com/SMI/SmiServices/pull/2025) by rkm. Add `DisableImplicitNuGetFallbackFolder` to work around https://github.com/NuGet/Home/issues/7921
- [#2044](https://github.com/SMI/SmiServices/pull/2044) by rkm. Add missing IsPooledExtraction property to ExtractMessage.java

### Change

- [#2023](https://github.com/SMI/SmiServices/pull/2023) by rkm. Centralise test logging setup into LoggingFixture
- [#2045](https://github.com/SMI/SmiServices/pull/2045) by rkm. Make modality required for all extractions

### Meta

- [#2007](https://github.com/SMI/SmiServices/pull/2007) by rkm. Enable Dependabot updates for dotnet-sdk
- [#2015](https://github.com/SMI/SmiServices/pull/2015) by rkm. Fix runtime selection in helper scripts
- [#2022](https://github.com/SMI/SmiServices/pull/2022) by rkm. Add pre-commit python hooks & tidy helper scripts
- [#2072](https://github.com/SMI/SmiServices/pull/2072) by rkm. Tidy-up code & add style rules

## [5.10.3] 2024-11-18

### Bugfix

- [#2003](https://github.com/SMI/SmiServices/pull/2003) by rkm. Fix UpdateValues/Updater to work with postgres

### Meta

- [#1997](https://github.com/SMI/SmiServices/pull/1997) by rkm. Specify RuntimeIdentifier in Directory.Build.props so VS knows which one to pick. Required since SelfContained was set

## [5.10.2] 2024-11-17

### Bugfix

- [#1996](https://github.com/SMI/SmiServices/pull/1996) by rkm. Ensure MapperSource properly quotes values for postgres

## [5.10.1] 2024-11-15

### Bugfix

- [#1993](https://github.com/SMI/SmiServices/pull/1993) by rkm. Add `SelfContained` property, now required for .NET 8+

## [5.10.0] 2024-11-15

### Bugfix

- [#1956](https://github.com/SMI/SmiServices/pull/1956) by rkm. Publish SmiServices only to fix NETSDK1194 warning
- [#1962](https://github.com/SMI/SmiServices/pull/1962) by rkm. Fixup namespaces in test projects
- [#1963](https://github.com/SMI/SmiServices/pull/1963) by rkm. Set MessageHeader.CurrentProgramName once at the start of each test fixture (project / assembly) instead of individually in each test. Fixes cases where tests would fail if run individually.
- [#1991](https://github.com/SMI/SmiServices/pull/1991) by rkm. Support postgres in IdentifierMapper server discovery

### Change

- [#1847](https://github.com/SMI/SmiServices/pull/1847) by rkm. Enable MSBuildTreatWarningsAsErrors and warnaserror

- [#1957](https://github.com/SMI/SmiServices/pull/1957) by rkm. Refactor project path resolvers in CohortExtractor:

  - `DefaultProjectPathResolver` is now `StudySeriesOriginalFilenameProjectPathResolver`
  - Undo handling UIDs with leading dot (#1506) as this was causing difficulties with path lookups elsewhere
  - Add `StudySeriesSOPProjectPathResolver` which generates filenames using SOPInstanceUID instead of the original file name. This is now the default path resolver
  - Disallow null Study/Series/SOPInstanceUID values, which should never occur in practice

### Meta

- [#1875](https://github.com/SMI/SmiServices/pull/1875) by rkm. Enable Nuget packages restore with lockfile

## [5.9.0] 2024-10-03

### Feature

- [#1945](https://github.com/SMI/SmiServices/pull/1945) by rkm. Add a publish timeout backoff mechanism to ProducerModel, allowing control over message publishing timeout behaviour. This can be enabled by setting `BackoffProviderType` in any `ProducerOptions` config. Currently implemented types are:

  - StaticBackoffProvider (1 minute flat timeout)
  - ExponentialBackoffProvider (1 minute initial, doubling after each timeout)

- [#1952](https://github.com/SMI/SmiServices/pull/1952) by rkm. Enable pausing of message publishing based on monitoring downstream queue message count

### Bugfix

- [#1954](https://github.com/SMI/SmiServices/pull/1954) by rkm. Fix program names in MessageHeader and logging setup

## [5.8.0] 2024-09-19

### Feature

- [#1938](https://github.com/SMI/SmiServices/pull/1938) by rkm. Allow specifying path to DynamicRules file

### Bugfix

- [#1892](https://github.com/SMI/SmiServices/pull/1892) by rkm. `docker-compose` replaced by `docker compose`
- [#1917](https://github.com/SMI/SmiServices/pull/1917) by rkm. Fix handling of GlobalOptions deserializing to null
- [#1939](https://github.com/SMI/SmiServices/pull/1939) by rkm. Fix loading of TesseractEngine on Linux
- [#1940](https://github.com/SMI/SmiServices/pull/1940) by rkm. Bump sqlserver to 2022 in CI to fix startup crash

### Change

- [#1879](https://github.com/SMI/SmiServices/pull/1879) by jas88. Remove Oracle remnants, fill in some gaps in Postgresql support instead
- [#1894](https://github.com/SMI/SmiServices/pull/1894) by rkm. Condense all code into 2 csprojs
- [#1901](https://github.com/SMI/SmiServices/pull/1901) by rkm. Switch coverage collection from coverlet.msbuild to Microsoft.CodeCoverage and dotnet-coverage
- [#1902](https://github.com/SMI/SmiServices/pull/1902) by rkm. Separate unit and integration tests into separate projects
- [#1903](https://github.com/SMI/SmiServices/pull/1903) by rkm. Run automated Visual Studio code cleanup & apply fixes
- [#1906](https://github.com/SMI/SmiServices/pull/1906) by jas88. Exclude test code from test coverage calculations since testing tests makes no sense

### Meta

- [#1891](https://github.com/SMI/SmiServices/pull/1891) by rkm. Tidy & auto-format codebase

## [5.7.2] 2024-07-29

### Feature

- [#1889](https://github.com/SMI/SmiServices/pull/1889) by jas88. Make DB exception handling DB-agnostic rather than MySQL specific

### Bugfix

- [#1887](https://github.com/SMI/SmiServices/pull/1887) by rkm. Pass setInitialDatabase as true to GetDistinctLiveDatabaseServer to ensure that the correct database is used when connecting to Postgres
- [#1888](https://github.com/SMI/SmiServices/pull/1888) by rkm. Ensure keyTag is properly wrapped in QuerySyntaxHelper for Postgres support

## [5.7.1] 2024-07-24

### Feature

- [#1876](https://github.com/SMI/SmiServices/pull/1876) by rkm. Manually bump DicomTypeTranslation, IsIdentifiable, YamlDotNet, and RDMP libs

## [5.7.0] 2024-06-24

### Feature

- [#1679](https://github.com/SMI/SmiServices/pull/1679) by darshad-github. Add dicom-anonymiser (Generic Wrapper Microservice)
- [#1723](https://github.com/SMI/SmiServices/pull/1723) by rkm. bump CI image to ubuntu-22.04
- [#1748](https://github.com/SMI/SmiServices/pull/1748) by howff. Add support for Dermatology SRs
- [#1823](https://github.com/SMI/SmiServices/pull/1823) by rkm. Bump to .NET 8.0 and C# 12
- [#1833](https://github.com/SMI/SmiServices/pull/1833) by jas88. Move SecurityCodeScan.VS2019 analysis to run as part of CodeQL CI checks not in IDE
- [#1834](https://github.com/SMI/SmiServices/pull/1834) by jas88. Update to NUnit v4 API
- [#1843](https://github.com/SMI/SmiServices/pull/1843) by jas88. Update DicomLoader switches and docs
- [#1844](https://github.com/SMI/SmiServices/pull/1844) by rkm. Tidy some files in DicomAnonymiser
- [#1845](https://github.com/SMI/SmiServices/pull/1845) by darshad-github. Update dicom-anonymiser docs
- [#1848](https://github.com/SMI/SmiServices/pull/1848) by rkm. replace archived prettier mirror with mdformat

### Bugfix

- [#1840](https://github.com/SMI/SmiServices/pull/1840) by jas88. Escape multiline CSV properly via CsvHelper

### Doc

- [#1831](https://github.com/SMI/SmiServices/pull/1831) by rkm. Refresh all documentation

  - Simplify the top-level README, as this content has been migrated to SMI/docs
  - Move READMEs for all tools inside the top-level docs/ to improve navigation
  - Add script for checking docs
  - Add markdown-link-check hook
  - Delete unused TriggerUpdates MongoDB source

### Removal

- [#1804](https://github.com/SMI/SmiServices/pull/1804) by rkm. Delete SRAnonTool, which has been migrated to https://github.com/SMI/StructuredReports.

## [5.6.1] 2024-04-19

### Bugfix

- [#1799](https://github.com/SMI/SmiServices/pull/1799) by rkm. Re-add a parameterless constructor to DynamicRejector so it can be loaded via reflection in CohortExtractorHost.

## [5.6.0] 2024-04-16

### Feature

- [#1735](https://github.com/SMI/SmiServices/pull/1735) by rkm. Add support for CohortPackager to process ExtractedFileVerificationMessages in batches
  - Enabled with `CohortPackagerOptions.VerificationMessageQueueProcessBatches`
  - Process rate configured with `CohortPackagerOptions.VerificationMessageQueueFlushTimeSeconds`. Defaults to 5 seconds if not set
- [#1746](https://github.com/SMI/SmiServices/pull/1746) by rkm. Start to refactor `RabbitMqAdapter` logic into generic interface
  - Rename IRabbitMqAdapter -> IMessageBroker
  - Move into `Smi.Common.Messaging` namespace
  - Add `MessageBrokerType` and `MessageBrokerFactory`
  - Create ConnectionFactory directly in `RabbitMQBroker`
  - Tidy unused variables and naming
- [#1749](https://github.com/SMI/SmiServices/pull/1749) by rkm. Refactor and simplify extraction reports
  - Generate a single `FailureStoreReport` which can be further processed depending on the need
  - Removed `ReportFormat` and `ReporterType` from `CohortPackagerOptions`
  - Merge `JobReporterBase` and `FileReporter` into `JobReporter`

### Bugfix

- [#1703](https://github.com/SMI/SmiServices/pull/1703) by jas88. Update to fix issues in installation script in docs

### Change

- [#1729](https://github.com/SMI/SmiServices/pull/1729) by rkm. Rename branch references from `master` to `main`

### Removal

- [#1730](https://github.com/SMI/SmiServices/pull/1730) by rkm. Disable services from Windows CI
- [#1747](https://github.com/SMI/SmiServices/pull/1747) by rkm. Remove unused `JetBrains.Annotations` package

## [5.5.0] 2024-01-18

### Feature

- [#1583](https://github.com/SMI/SmiServices/pull/1583) by rkm. Add DynamicRulesTester application
- [#1599](https://github.com/SMI/SmiServices/pull/1599) by rkm. Enable the C# `nullable` feature and fix all warnings.
- [#1619](https://github.com/SMI/SmiServices/pull/1619) by rkm. Add Modality to ExtractFileMessage classes
- [#1622](https://github.com/SMI/SmiServices/pull/1622) by rkm. Add LossyImageCompressionMethod to CTP allowlist
- [#1637](https://github.com/SMI/SmiServices/pull/1637) by rkm. upgrade to NET7 and C# 11
- [#1663](https://github.com/SMI/SmiServices/pull/1663) by karacolada. Add field UserName to ExtractionRequestInfoMessage
- [#1701](https://github.com/SMI/SmiServices/pull/1701) by rkm. Adds the ability for consumers to optionally "hold" unprocessable messages so they are not returned to the queue.

### Bugfix

- [#1524](https://github.com/SMI/SmiServices/pull/1524) by howff. StructuredReport improvements - collect names from anywhere in text body not just in header
- [#1562](https://github.com/SMI/SmiServices/pull/1562) by howff. DicomText - Redact all tags which have a data type (VR) of 'DT' (DateTime). It was already doing dates and names.
- [#1611](https://github.com/SMI/SmiServices/pull/1611) by howff. CTP_SRAnonTool - implement a full HTML parser (with other sanity checks) for HTML in TextValue in SRs
- [#1639](https://github.com/SMI/SmiServices/pull/1639) by rkm. catch any exception raised during file classification. Fixes #1638
- [#1671](https://github.com/SMI/SmiServices/pull/1671) by rkm. disable debug logging spam from external CTP libraries
- [#1684](https://github.com/SMI/SmiServices/pull/1684) by jas88. Fix SQL in documentation area - missing semicolons causing syntax error
- [#1704](https://github.com/SMI/SmiServices/pull/1704) by rkm. Improve quality of log output
- [#1717](https://github.com/SMI/SmiServices/pull/1717) by rkm. update CI release script for upload-artifact@v4

### Docs

- [#1572](https://github.com/SMI/SmiServices/pull/1572) by jas88. Document installation steps for new users

### Removal

- [#1540](https://github.com/SMI/SmiServices/pull/1540) by rkm. Remove old IsIdentifiableReviewer project, moved to https://github.com/SMI/IsIdentifiable

### Update

- [#1664](https://github.com/SMI/SmiServices/pull/1664) by jas88. Update RDMP API to reduce casting

## [5.4.0] 2023-04-25

### Feature

- [#1489](https://github.com/SMI/SmiServices/pull/1489) by howff. CTP_DicomToText has options to control the output formatting (especially redaction of HTML and replacement of newlines)

### Bugfix

- [#1379](https://github.com/SMI/SmiServices/pull/1379) by rkm. Constrain protobuf version to "\<3.20.0", since this is the last version which supports python3.6 and we only have 3.6 on the live system.
- [#1396](https://github.com/SMI/SmiServices/pull/1396) by jas88. Fix some issues encountered loading DICOM to Mongo in HIC deployment
- [#1399](https://github.com/SMI/SmiServices/pull/1399) by jas88. Revert to setup-java@3.6.0 due to disappearance of 3.7.0
- [#1430](https://github.com/SMI/SmiServices/pull/1430) by rkm. Add RabbitMQ management port to docker-compose files
- [#1431](https://github.com/SMI/SmiServices/pull/1431) by rkm. restrict mysql_connector_python version for python 3.6
- [#1448](https://github.com/SMI/SmiServices/pull/1448) by rkm. Add 0.1% threshold for Codecov to report CI failure
- [#1492](https://github.com/SMI/SmiServices/pull/1492) by rkm. add codecov token to CI
- [#1493](https://github.com/SMI/SmiServices/pull/1493) by rkm. ignore BasicRules.yaml in pre-commit codespell config
- [#1497](https://github.com/SMI/SmiServices/pull/1497) by rkm. Fix MongoDB Windows CI by changing to sc.exe
- [#1506](https://github.com/SMI/SmiServices/pull/1506) by darshad-github. Fix issue with hidden directories in extraction output paths generated by cohort extractor.
- [#1509](https://github.com/SMI/SmiServices/pull/1509) by jas88. Change Windows service control operation timeout to 300s since Mongo sometimes takes longer than the default 30s causing timeout errors otherwise

### Doc

- [#1499](https://github.com/SMI/SmiServices/pull/1499) by karacolada. update CTP build docs

### Meta

- [#1401](https://github.com/SMI/SmiServices/pull/1401) by rkm. Condense formatting options into .editorconfig and add more hooks
- [#1407](https://github.com/SMI/SmiServices/pull/1407) by rkm. Bump SDK to 6.0.403
- [#1428](https://github.com/SMI/SmiServices/pull/1428) by rkm. Add development support for ARM / M1 Mac
- [#1442](https://github.com/SMI/SmiServices/pull/1442) by rkm. Switch coverage to Codecov
- [#1450](https://github.com/SMI/SmiServices/pull/1450) by darshad-github. Update contributing.md with feature branch workflow guidelines
- [#1451](https://github.com/SMI/SmiServices/pull/1451) by rkm. adopt dotnet central package management (CPM)
- [#1468](https://github.com/SMI/SmiServices/pull/1468) by rkm. switch rabbitmq docker-compose images to include management plugin
- [#1494](https://github.com/SMI/SmiServices/pull/1494) by rkm. [actions] replace deprecated set-output with GITHUB_OUTPUT
- [#1496](https://github.com/SMI/SmiServices/pull/1496) by rkm. add CodeQL scan

## [5.3.0] 2022-11-08

### Feature

- [#1259](https://github.com/SMI/SmiServices/pull/1259) by rkm. Add support
  for extraction processing failures
  - Remove `IsIdentifiable` field from `ExtractedFileVerificationMessage`
    and replace with new `VerifiedFileStatus` enum
  - Update extraction job classes in `CohortPackager` to store this new
    field, and handle backwards-incompatibility when reading older
    extraction logs

### Bugfix

- [#1285](https://github.com/SMI/SmiServices/pull/1285) by howff. Improve the
  removal of HTML tags from StructuredReport (SR) text
- [#1314](https://github.com/SMI/SmiServices/pull/1314) by jas88. Replace
  SharpCompress usage due to buggy LZMA handling to fix issue #1313
- [#1350](https://github.com/SMI/SmiServices/pull/1350) by tznind. Fix
  DicomRelationalMapper when running with a YamlRepository backend

### Change

- [#1270](https://github.com/SMI/SmiServices/pull/1270) by tznind. Rename yaml
  config file `IsIdentifiableBaseOptions` to `IsIdentifiableOptions` and
  removed unused CLI verbs in `smi`

### Meta

- [#1261](https://github.com/SMI/SmiServices/pull/1261) by jas88. Update
  caching strategy in CI

## [5.2.0] 2022-08-10

### Feature

- [#1254](https://github.com/SMI/SmiServices/pull/1254) by tznind. Change
  "Setup" to a library and make it runnable from smi as verb
- [#1255](https://github.com/SMI/SmiServices/pull/1255) by tznind. Add support
  for running RDMP with YamlRepository backend

### Bugfix

- [#1241](https://github.com/SMI/SmiServices/pull/1241) by rkm. Refactor
  IsIdentifiableQueueConsumer
  - Improve exception handling to better handle errors caused by IClassifier
  - Remove redundant `fileSystemRoot` from constructor
  - Add tests
- [#1256](https://github.com/SMI/SmiServices/pull/1256) by rkm. Ensure MongoDB
  service started in Windows CI. Caused by
  https://github.com/actions/runner-images/issues/5949.

### Removal

- [#1251](https://github.com/SMI/SmiServices/pull/1251) by rkm. Remove
  remaining IsIdentifiable code and CI config

## [5.1.3] 2022-07-21

- [#1229](https://github.com/SMI/SmiServices/pull/1229) by dependabot. Bump
  IsIdentifiable from 0.0.4 to 0.0.5 to fix broken API

## [5.1.2] 2022-07-18

### Bugfix

- [#1223](https://github.com/SMI/SmiServices/pull/1223) by rkm. Fix setup.py
  in Smi_Common_Python to include all files, regardless of the current
  directory.

## [5.1.1] 2022-07-18

### Bugfix

- [#1216](https://github.com/SMI/SmiServices/pull/1216) by jas88. Bugfix:
  issue #1217, microservices prematurely exiting since RabbitMQ no longer
  delays completion

## [5.1.0] 2022-06-23

### Feature

- [#1074](https://github.com/SMI/SmiServices/pull/1074) by rkm. Switch CI to
  GitHub Actions
- [#1134](https://github.com/SMI/SmiServices/pull/1134) by jas88. Remove
  DeadLetterReprocessor and associated code, more elegant native RabbitMQ
  approach available if needed.
- [#1190](https://github.com/SMI/SmiServices/pull/1190) by howff. Anonymise
  names and dates in SRs, just in case (useful for demo). Proceed without ID
  mapping if DB unavailable (useful for testing).
- [#1199](https://github.com/SMI/SmiServices/pull/1199) by tznind. Added Setup
  utility for checking config settings (connection strings, queue setup etc)
- [#1200](https://github.com/SMI/SmiServices/pull/1200) by jas88. Add new
  DicomLoader CLI application for batch-loading DICOM files and archives

### Bugfix

- [#1087](https://github.com/SMI/SmiServices/pull/1087) by rkm. [CI] Misc. CI
  fixes
  - Fixes the build scripts to respect any intermediate non-zero return
    codes
  - Fixes the build scripts to only build `linux-x64` and `win-x64`
  - Fixes the build scripts to select the correct build configuration
  - Removes a bogus test leftover from #1089
  - Temporarily disables a few tests requiring a fix for the `leptonica`
    libs
- [#1088](https://github.com/SMI/SmiServices/pull/1088) by rkm. Use
  DirectConnection instead of obsolete ConnectionMode. Fixes #990
- [#1089](https://github.com/SMI/SmiServices/pull/1089) by rkm. Remove invalid
  extraction modality check. Fixes #1059
- [#1128](https://github.com/SMI/SmiServices/pull/1128) by jas88. Update
  RabbitMQ Nuget package and associated API calls
- [#1138](https://github.com/SMI/SmiServices/pull/1138) by jas88. Use
  Chocolatey to install SQL 2019 LocalDB instead of Powershell script
- [#929](https://github.com/SMI/SmiServices/pull/929) by howff. Structured
  Reports improvements from PR#929
  - Updated documentation
  - Simplify SRAnonTool using external program semehr_anon.py
  - Handle ConceptNameCodeSequence which has VR but no Value
  - Ensure 'replaced' flag is not reset
  - Write replacement DICOM whichever content tag is found
  - Extract metadata from Mongo to go alongside anonymised text
  - Redact numeric DICOM tags with all '9' not all 'X'
  - Allow badly-formatted text content which contains HTML but does not
    escape non-HTML symbols

### Change

- [#1023](https://github.com/SMI/SmiServices/pull/1023) by jas88. RabbitMQ
  tidyup
  - Fix both C# and Java server version check logic
  - Reuse Connection more per RabbitMQ guidance
  - Tidy up some Java exception handling logic

### Meta

- [#1082](https://github.com/SMI/SmiServices/pull/1082) by rkm. CI: Don't exit
  early if a single test project has failures

## [5.0.1] 2022-02-18

### Bugfix

- [#1054](https://github.com/SMI/SmiServices/pull/1054) by rkm. Swap stderr
  reading and process.waitFor in SmiCtpProcessor to avoid a deadlock

## [5.0.0] 2022-02-17

### Feature

- [#1040](https://github.com/SMI/SmiServices/pull/1040) by tznind. Updated to
  latest dotnet sdk (net6)
- [#973](https://github.com/SMI/SmiServices/pull/973) by rkm. Adds the basis
  for a new "DicomAnonymiser" microservice, which can be used in place of the
  existing Java CTP service. It supports pluggable anonymisers through the
  `IDicomAnonymiser` interface. No implementations are provided in this PR.

### Bugfix

- [#1010](https://github.com/SMI/SmiServices/pull/1010) by rkm. Update CI
  script to pull tessdata file from the new branch name (main not master)
- [#1050](https://github.com/SMI/SmiServices/pull/1050) by rkm. Disable
  coverage upload which is currently broken for .NET 6
- [#1052](https://github.com/SMI/SmiServices/pull/1052) by rkm. Ensure any
  errors encountered when running SRAnonTool are handled properly and produce
  an appropriate status message.
- [#885](https://github.com/SMI/SmiServices/pull/885) by howff. Always convert
  PHI to XML regardless of annotation_mode setting.
- [#914](https://github.com/SMI/SmiServices/pull/914) by rkm. Fix #913; error
  if file extraction command is re-run after being cancelled
- [#915](https://github.com/SMI/SmiServices/pull/915) by rkm. Fix #891; jobId
  not created until extraction is completed.
- [#917](https://github.com/SMI/SmiServices/pull/917) by rkm. Ensure control
  exchange exists for ExtractImagesHostTests
- [#926](https://github.com/SMI/SmiServices/pull/926) by rkm. Fixes the
  current CI issues by restricting the creation of the ControlExchange to the
  RequiresRabbit test decorator.
- [#931](https://github.com/SMI/SmiServices/pull/931) by rkm. Ensure the
  python version used in CI runs is exactly what we specify
- [#968](https://github.com/SMI/SmiServices/pull/968) by rkm. Remove reference
  to System.Drawing.Common
- [#976](https://github.com/SMI/SmiServices/pull/976) by jas88. Fix issue
  #921 - erroneous stripping of root path to empty string in GlobalOptions
- [#981](https://github.com/SMI/SmiServices/pull/981) by rkm. Fix deprecated
  python collections import for py310+
- [#987](https://github.com/SMI/SmiServices/pull/987) by rkm. Treat all build
  warnings as errors, and fix or disable existing ones. Also remove unused
  System.Security.AccessControl package.

### Meta

- [#923](https://github.com/SMI/SmiServices/pull/923) by rkm. Allow newer
  minor SDK versions to build the sln. global.json specifies the minimum
  version which will be used in the CI.
- [#986](https://github.com/SMI/SmiServices/pull/986) by rkm. Add missing
  reference to NLog.

## [4.0.0] 2021-08-09

### Feature

- [#849](https://github.com/SMI/SmiServices/pull/849) by jas88. CTPAnonymiser
  refactoring
  - Reduce memory footprint (issue #837)
  - Simplify RabbitMQ message handling
  - Stop creating temporary copy of input file - no longer needed in EPCC
    environment without Lustre FS (per issue #836)
  - Add checks input file is readable not just extant, hopefully fixing
    issue #533
- [#861](https://github.com/SMI/SmiServices/pull/861) by rkm. Add Equ to
  automatically implement equality members for classes.
- [#878](https://github.com/SMI/SmiServices/pull/878) by rkm. Update RDMP
  packages with replacement of System.Data.SqlClient with
  Microsoft.Data.SqlClient. Replace usages of same in codebase

### Bugfix

- [#764](https://github.com/SMI/SmiServices/pull/764) by howff. Clean up the
  Python code lint after running pylint3
- [#841](https://github.com/SMI/SmiServices/pull/841) by tznind. Fixed bug
  when disposing `CsvDestination` instances that have not begun writing any
  output
- [#880](https://github.com/SMI/SmiServices/pull/880) by tznind. Fixed edge
  case in IdentifierMapper when a dicom tag has illegal multiplicity in
  PatientID field

### Meta

- [#843](https://github.com/SMI/SmiServices/pull/843) by rkm. Add pre-commit
  and codespell. Fix all current spelling mistakes
- [#844](https://github.com/SMI/SmiServices/pull/844) by rkm. Fixup regex in
  codespell config
- [#855](https://github.com/SMI/SmiServices/pull/855) by rkm. Specify dotnet
  SDK version in global.json
- [#859](https://github.com/SMI/SmiServices/pull/859) by rkm. Bump LangVersion
  to 9.0
- [#876](https://github.com/SMI/SmiServices/pull/876) by rkm. Add check and
  error message for missing coveralls token
- [#877](https://github.com/SMI/SmiServices/pull/877) by rkm. Fix setting
  replication for MongoDB in Windows CI pipelines

### Removal

- [#848](https://github.com/SMI/SmiServices/pull/848) by rkm. Removed
  NationalPACSAccessionNumber from all metadata

## [3.2.1] 2021-07-07

### Bugfix

- [#834](https://github.com/SMI/SmiServices/pull/834) by tznind. Improved
  logging and fixed yaml options not being respected in IsIdentifiableReviewer

## [3.2.0] 2021-07-05

### Feature

- [#795](https://github.com/SMI/SmiServices/pull/795) by tznind. Added support
  for specifying IsIdentifiable CLI options in the yaml config files instead
  of command line (command line will always take precedence if both are
  specified)
- [#797](https://github.com/SMI/SmiServices/pull/797) by tznind. Added custom
  themes to IsIdentifiableReviewer. Use flag `--theme mytheme.yaml`
- [#801](https://github.com/SMI/SmiServices/pull/801) by tznind. Added help
  and cancel buttons to custom pattern dialog in reviewer
- [#802](https://github.com/SMI/SmiServices/pull/802) by tznind. Added
  IsIdentifiableReviewer settings into main yaml config
- [#818](https://github.com/SMI/SmiServices/pull/818) by tznind. Added Rules
  Manager to IsIdentifiable Reviewer

### Bugfix

- [#787](https://github.com/SMI/SmiServices/pull/787) by rkm. Fix the call to
  the release changelog script
- [#788](https://github.com/SMI/SmiServices/pull/788) by rkm. Require all CI
  tests to pass before packaging runs
- [#789](https://github.com/SMI/SmiServices/pull/789) by rkm. Don't upload
  coverage for tagged builds
- [#796](https://github.com/SMI/SmiServices/pull/796) by tznind. Fixed bug
  opening corrupted reports in IsIdentifiableReviewer crashing the application
- [#806](https://github.com/SMI/SmiServices/pull/806) by tznind. Replace use
  of GlobalColorScheme with the proper static members in Terminal.Gui that
  will propagate correctly for all windows without having to set them
  manually.
- [#811](https://github.com/SMI/SmiServices/pull/811) by rkm. Fix coverage
  task always running even if a previous task failed

### Doc

- [#823](https://github.com/SMI/SmiServices/pull/823) by tznind. Refresh
  documentation for IsIdentifiableReviewer

## [3.1.0] 2021-06-11

### Feature

- [#759](https://github.com/SMI/SmiServices/pull/759) by tznind. Added
  parallelisation to load process in IsIdentifiableReviewer rules view

### Bugfix

- [#756](https://github.com/SMI/SmiServices/pull/756) by howff. Open CSV file
  read-only
- [#771](https://github.com/SMI/SmiServices/pull/771) by tznind.
  IsIdentifiableReviewer:
  - Added --usc (UseSystemConsole) for alternative display driver based on
    System.Console
  - Removed modal dialog that could cause errors opening a previously
    completed report
  - Added label with currently opened file and fixed ignore/update labels
  - Added spinner indicator for when loading the Next report in sequential
    mode takes a while
  - Fixed bug where Ctrl+Q in Ignore/Update with custom patterns in
    RulesView results in hard crash
- [#785](https://github.com/SMI/SmiServices/pull/785) by tznind. Fixed bug
  with multiple enumeration during loading very large failure reports in
  IsIdentifiableReviewer

### Meta

- [#773](https://github.com/SMI/SmiServices/pull/773) by rkm. Add code
  coverage
- [#775](https://github.com/SMI/SmiServices/pull/775) by rkm. Move useful
  scripts from .azure-pipelines/scripts to utils. Update utils/README.md.
- [#781](https://github.com/SMI/SmiServices/pull/781) by rkm. Fixup coverage
  variables between pushes/PRs

## [3.0.2] 2021-05-14

### Bugfix

- [#745](https://github.com/SMI/SmiServices/pull/745) by tznind. Fixed
  reviewer tree view refresh/async code

## [3.0.1] 2021-05-06

### Feature

- [#738](https://github.com/SMI/SmiServices/pull/738) by rkm. Improvements to
  CLI user experience
  - Immediately verify that the config file (GlobalOptions) we've loaded is
    somewhat valid
  - Improve visibility of exception messages on CLI exit

### Bugfix

- [#737](https://github.com/SMI/SmiServices/pull/737) by rkm. Switch ordering
  of annotations in ExtractImagesCliOption to fix a runtime exception.

### Meta

- [#736](https://github.com/SMI/SmiServices/pull/736) by rkm. Remove .NET Core
  2.2 runtime from the Azure Pipelines builds

## [3.0.0] 2021-05-06

### Feature

- [#702](https://github.com/SMI/SmiServices/pull/702) by rkm. Replace Java
  ExtractorCLI with C# ExtractImages service
  - _Breaking_ Existing scripts and documentation
  - _Breaking_ Change ExtractorClOptions -> ExtractImagesOptions in YAML
    configs
- [#713](https://github.com/SMI/SmiServices/pull/713) by rkm. Upgrade all
  projects and related CI scripts to `net5`.
- [#734](https://github.com/SMI/SmiServices/pull/734) by tznind. Updated to
  RDMP 5.0.0 (and Dicom Plugin 3.0.0)

### Bugfix

- [#708](https://github.com/SMI/SmiServices/pull/708) by rkm. Add nuget.config
  file to fix flaky issues with Azure CI Runners. Ref:
  - https://github.com/NuGet/Home/issues/10586
  - https://github.com/actions/virtual-environments/issues/3038
- [#714](https://github.com/SMI/SmiServices/pull/714) by rkm. Fixup current
  LGTM alerts.
- [#715](https://github.com/SMI/SmiServices/pull/715) by rkm. Revert to the
  hack-y method of fixing the Nuget cache for Azure Windows agents.
- [#722](https://github.com/SMI/SmiServices/pull/722) by rkm. Remove
  workaround for Windows agents in Azure CI since fixed upstream. Ref:
  - https://github.com/actions/virtual-environments/issues/3038

## [2.1.1] 2021-04-07

### Bugfix

- [#697](https://github.com/SMI/SmiServices/pull/697) by rkm. Fixes #695.
  Removes the checks preventing modality being specified with other extraction
  keys
- [#698](https://github.com/SMI/SmiServices/pull/698) by rkm. CohortExtractor
  database queries that crash during execution are now logged
- [#701](https://github.com/SMI/SmiServices/pull/701) by howff. Several
  improvements to Python code for handling unusually-formatted SR documents.
- [#704](https://github.com/SMI/SmiServices/pull/704) by rkm. Fix
  ReportNewLine being incorrectly set to a pre-escaped string. Fixes #703

### Doc

- [#672](https://github.com/SMI/SmiServices/pull/672) by howff.
  IsIdentifiableReviewer document updated

## [2.1.0] 2021-03-30

### Feature

- [#676](https://github.com/SMI/SmiServices/pull/676) by tznind. Improvements
  to IsIdentifiable Reviewer
  - Tab based navigation
  - Better pattern generation for overlapping failure parts
  - Fixed sequential mode always showing failures covered by existing UPDATE
    rules
  - Fixed tree view not adapting as new rules are added e.g. when used to
    interactively process many failures
- [#690](https://github.com/SMI/SmiServices/pull/690) by tznind. Added
  ModalitySpecificRejectorOptions

### Bugfix

- [#678](https://github.com/SMI/SmiServices/pull/678) by rkm. Fix log
  directory naming for single-entrypoint app. Fixes #677
- [#681](https://github.com/SMI/SmiServices/pull/681) by howff. Fix Python
  packaging when run from repo root dir
- [#684](https://github.com/SMI/SmiServices/pull/684) by howff.
  - Minor fixes to python for compatibility (eg. semehr using python2)
  - Use a unique temporary directory for input and output files
  - Enable extraction from MongoDB by StudyDate (the only field other than
    FilePath which has an index)
- [#688](https://github.com/SMI/SmiServices/pull/688) by howff. Reword the
  CohortExtractor log/info messages as it's not just patients which can be
  rejected

### Meta

- [#668](https://github.com/SMI/SmiServices/pull/668) by rkm. Add note to the
  releasing documentation to alert the Mattermost channel

## [2.0.0] 2021-03-13

### Feature

- [#618](https://github.com/SMI/SmiServices/pull/618) by tznind.
  IsIdentifiableReviewer
  - Added progress when loading large files (with cancellation support)
  - Now groups outstanding failures by column
  - Fixed rules being flagged as 'Identical' when classifying different
    input columns
- [#634](https://github.com/SMI/SmiServices/pull/634) by rkm. Convert to
  single entry-point app
  - Breaking: Existing scripts and processes which reference the old
    applications
- [#636](https://github.com/SMI/SmiServices/pull/636) by howff. Improvements
  to Python scripts, tests, documentation
- [#647](https://github.com/SMI/SmiServices/pull/647) by howff. Improved
  testing of SR anonymiser with a standalone stub
- [#662](https://github.com/SMI/SmiServices/pull/662) by tznind. Added ability
  to ignore multiple failures at once in Is Identifiable Reviewer

### Bugfix

- [#597](https://github.com/SMI/SmiServices/pull/597) by tznind. Fixed
  ConsensusRules not being run
- [#619](https://github.com/SMI/SmiServices/pull/619) by jas88. Reduce memory
  consumption in nerd
- [#632](https://github.com/SMI/SmiServices/pull/632) by rkm. Normalise the
  IsIdentifiableReviewer namespace to match the other Applications
- [#646](https://github.com/SMI/SmiServices/pull/646) by howff. Call the
  garbage collector (and report progress) every 1000 records processed from a
  database source in IsIdentifiable.
- [#650](https://github.com/SMI/SmiServices/pull/650) by howff. Create python
  package called SmiServices so a wheel can be created.
  - Rename all the imports for the new package name
  - Remove references to PYTHONPATH Replace all paths and relative imports
    to be independent of current directory Fixes to python tests
- [#656](https://github.com/SMI/SmiServices/pull/656) by howff. SRAnonTool
  updated to handle the output from the latest SemEHR anonymiser and ignore
  None-type annotations
- [#661](https://github.com/SMI/SmiServices/pull/661) by tznind. Fixed layout
  of main window so it no longer obscures classification/type
- [#665](https://github.com/SMI/SmiServices/pull/665) by tznind. Fixed tree
  view losing selected index when updating/ignoring a failure (in tree view)
- [#666](https://github.com/SMI/SmiServices/pull/666) by howff. Silence
  deprecation warning from newer Python as noted by the azure pipeline test

### Meta

- [#588](https://github.com/SMI/SmiServices/pull/588) by rkm. Prevent
  additional language packs being included in published packages. Reduces
  overall package size a bit.
- [#592](https://github.com/SMI/SmiServices/pull/592) by rkm. Manually update
  csvhelper to 25.0.0, fo-dicom to 4.0.7
- [#616](https://github.com/SMI/SmiServices/pull/616) by rkm. Check for
  clobbered files during package build
- [#620](https://github.com/SMI/SmiServices/pull/620) by rkm. Replace the
  legacy SecurityCodeScan with SecurityCodeScan.VS2019
- [#637](https://github.com/SMI/SmiServices/pull/637) by rkm. Change to
  tracking PR changes in news fragment files. Add a script to auto-update the
  CHANGELOG from these files.
- [#648](https://github.com/SMI/SmiServices/pull/648) by rkm. Remove the
  temporary reference to BadMedicine added in
  [#592](https://github.com/SMI/SmiServices/pull/592)
- [#654](https://github.com/SMI/SmiServices/pull/654) by howff. Add Azure
  Pipelines test and packaging for the Python scripts

## [1.15.1] 2021-02-17

### Fixed

- [#610](https://github.com/SMI/SmiServices/pull/610) by `howff`. Fixed CTP
  logging

## [1.15.0] 2021-02-17

### Changed

- [#575](https://github.com/SMI/SmiServices/pull/575) by `rkm`. Standardised
  logging setup and Program entries across whole solution
  - Breaking: YAML config change required
  - Removes the `SMI_LOGS_ROOT` variable - now in YAML config
  - Removes the `--trace-logging` CLI option - now in YAML config
  - All invocations of IsIdentifiable now require a YAML config to ensure
    logging is properly configured
- [#577](https://github.com/SMI/SmiServices/pull/577) by `rkm`. Simplify
  branch workflow by dropping develop

### Fixed

- [#581](https://github.com/SMI/SmiServices/pull/581) by `rkm`. Fixed a bug
  where newlines would never be correctly parsed from the config option in
  CohortPackager
- [#597](https://github.com/SMI/SmiServices/pull/597) by `tznind`. Fixed
  ConsensusRules not being run

### Dependencies

- Bump CsvHelper from 22.1.1 to 22.1.2
- Bump HIC.RDMP.Plugin from 4.2.3 to 4.2.4
- Bump HIC.RDMP.Plugin.Test from 4.2.3 to 4.2.4
- Bump Magick.NET-Q16-AnyCPU from 7.23.1 to 7.23.2
- Bump SecurityCodeScan from 3.5.3 to 3.5.4
- Bump System.Drawing.Common from 5.0.0 to 5.0.1
- Bump System.IO.Abstractions from 13.2.9 to 13.2.11
- Bump System.IO.Abstractions.TestingHelpers from 13.2.9 to 13.2.11
- Bump jansi from 2.2.0 to 2.3.1
- Bump junit from 4.13.1 to 4.13.2

## [1.14.1] - 2021-02-04

### Fixed

- [#576](https://github.com/SMI/SmiServices/pull/576) Fixup Windows package
  build

## [1.14.0] - 2021-02-04

### Added

- Added total job duration to extraction report header
- IsIdentifiableReviewer rule review screen
- Added CSV input support for IsIdentifiable, use verb `file` from command
  line
- Updater microservice now audits performance of queries (cumulative affected
  rows, queries executed etc)
- Added `-f` option to DicomTagReader to read a single zip/dicom file
- Added some Python library code

### Changed

- Clarified the CLI help text for `--format` in CohortPackager
- CTP calls an external program to anonymise Structured Reports
  - Requires an addition to `default.yaml`:
    `CTPAnonymiserOptions.SRAnonTool` (adding this does not break existing
    programs).
- Consolidate System.IO.Abstractions.TestingHelpers package references into
  the Smi.Common.Tests package
- Tidy common csproj options into `Directory.Build.props` files for all, src,
  and test projects
- Replace TravisCI and AppVeyor builds with Azure Pipelines

### Fixed

- CohortPackager: Don't try and create the jobId file when recreating an
  existing report
- CohortPackager.Tests: Fix a flaky test caused by NUnit setup/teardown code
  when running tests in parallel
- CohortPackager.Tests: Fix a flaky test caused by using the same MongoDB
  database name when running tests in parallel
- Fixed the existing CTPAnonymiser tests which had not been updated for the
  SRAnonTool changes
- Fixed executable name on UpdateValues microservice

### Dependencies

- Bump CsvHelper from 17.0.1 to 22.1.1
- Bump HIC.RDMP.Dicom from 2.1.11 to 2.2.2
- Bump HIC.RDMP.Plugin from 4.1.9 to 4.2.3
- Bump HIC.RDMP.Plugin.Test from 4.1.9 to 4.2.3
- Bump Magick.NET-Q16-AnyCPU 7.22.2.2 to 7.23.1
- Bump Microsoft.NET.Test.Sdk 16.8.0 to 16.8.3
- Bump Moq from 4.15.2 to 4.16.0
- Bump NUnit from 3.12.0 to 3.13.1
- Bump NunitXml.TestLogger from 2.1.80 to 3.0.91
- Bump System.IO.Abstractions from 13.2.2 to 13.2.9
- Bump System.IO.Abstractions.TestingHelpers from 13.2.2 to 13.2.9
- Bump YamlDotNet from 9.1.0 to 9.1.4

## [1.13.0] - 2020-12-03

### Added

- Added new command line application TriggerUpdates for detecting and issuing
  UpdateValuesMessages (e.g. ECHI mapping changes)
- Added new service UpdateValues which propagates changes (e.g. ECHI mapping
  changes) throughout the deployed database tables.
- ConsensusRule for combining 2+ other rules e.g. SocketRules (See
  IsIdentifiable Readme.md for more details)
- Added runtime and total failures count to IsIdentifiable logs
- Added NoSuffixProjectPathResolver which generates anonymous image path names
  that do not contain "-an" (which is the default behaviour).
  - To use, set `CohortExtractorOptions.ProjectPathResolverType` to
    `Microservices.CohortExtractor.Execution.ProjectPathResolvers.NoSuffixProjectPathResolver`
  - For identifiable extractions, the NoSuffixProjectPathResolver is now
    used
- Validation reports can now be created as either "Combined" (single report as
  before" or "Split" (a pack of reports
  including CSVs suitable for post-processing). This is configurable in the
  YAML config and can also be specified on the CLI when recreating reports for
  an extraction
- Added JobCompletedAt to the validation reports
- IsIdentifiable: Add support for ignoring OCR output less than `n` characters
  in length
- IsIdentifiable: Add a test case for burned-in image text

### Changed

- Update docs and make more keywords links to the relevant docs (#440)
- Reduce memory usage on long-running microservices even when .Net assumes RAM
  is plentiful
- Validation reports are now written to the project reports directory, instead
  of to a central reports directory

### Fixed

- Fix mismatch in Java/C# messages for ExtractionModality
- ExtractionFileCopier: Copy files relative to the extraction root not the
  global filesystem root
- Fix implementation of minimum OCR length (before being reported) #471

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

- Remove reference to MongoDB.Driver in Smi.Common.MongoDb.csproj since it
  caused a version conflict in the output packages

## [1.12.0] - 2020-09-14

### Added

- [breaking] Add identifiable extraction support
  - New service "FileCopier" which sits in place of CTP for identifiable
    extractions and copies source files to their output dirs
  - Changes to MongoDB extraction schema, but backwards compatibility has
    been tested
  - RabbitMQ extraction config has been refactored. Queues and service
    config files need to be updated
- Add [SecurityCodeScan](https://security-code-scan.github.io/) tool to build
  chain for .NET code
- Add "no filters" extraction support. If specified when running ExtractorCLI,
  no file rejection filters will be applied by CohortExtractor. True by
  default for identifiable extractions
- Added caching of values looked up in NLP/rulesbase for IsIdentifiable tool
- Added new rejector that throws out values (e.g. patient IDs) whose IDs are
  stored in a database table. Set `RejectColumnInfos` option in yaml to enable
  this
- Added a check to QueryToExecuteResult for RejectReason being null when
  Reject is true.

### Changed

- [breaking] Environment variables are no longer required. Previous settings
  now appear in configuration file
  - Environment variable `SMI_LOGS_ROOT` is now `GlobalOptions.LogsRoot`
  - Environment variable `MONGO_SERVICE_PASSWORD` is now
    `MongoDbOptions.Password`
  - Removed `ISIDENTIFIABLE_NUMTHREADS` as it didn't work correctly anyway
- Extraction report: Group PixelData separately and sort by length
- IsIdentifiable Reviewer 'Symbols' rule factory now supports digits only or
  characters only mode (e.g. use `\d` for digits but leave characters
  verbatim)
- IsIdentifiable Reviewer 'symbols' option when building Regex now builds
  capture groups and matches only the failing parts of the input string not
  the full ProblemValue. For example `MR Head 12-11-20` would return
  `(\d\d-\d\d-\d\d)$`

### Fixed

- Fix the extraction output directory to be
  `<projId>/extractions/<extractname>`

### Dependencies

- Bump fo-dicom.Drawing from 4.0.5 to 4.0.6
- Bump fo-dicom.NetCore from 4.0.5 to 4.0.6
- Bump HIC.BadMedicine.Dicom from 0.0.6 to 0.0.7
- Bump HIC.DicomTypeTranslation from 2.3.0 to 2.3.1
- Bump HIC.FAnsiSql from 1.0.2 to 1.0.5
- Bump HIC.RDMP.Dicom from 2.1.6 to 2.1.10
- Bump HIC.RDMP.Plugin from 4.1.6 to 4.1.8
- Bump HIC.RDMP.Plugin.Test from 4.1.6 to 4.1.8
- Bump Microsoft.CodeAnalysis.CSharp.Scripting from 3.6.0 to 3.7.0
- Bump Microsoft.Extensions.Caching.Memory from 3.1.6 to 3.1.8
- Bump Microsoft.NET.Test.Sdk from 16.6.1 to 16.7.1
- Bump MongoDB.Driver from 2.11.0 to 2.11.2
- Bump System.IO.Abstractions from 12.1.1 to 12.1.9
- Bump System.IO.Abstractions.TestingHelpers from 12.1.1 to 12.1.9
- Bump Terminal.Gui from 0.81.0 to 0.89.4

## [1.11.1] - 2020-08-12

- Set PublishTrimmed to false to fix bug with missing assemblies in prod.

## [1.11.0] - 2020-08-06

### Added

- DicomDirectoryProcessor and TagReader support for zip archives
  - Expressed in notation `/mydrive/myfolder/myzip.zip!somesubdir/my.dcm`
  - Requires command line `-f zips`

### Changed

- Improved the extraction report by summarising verification failures
- Start MongoDB in replication mode in the Travis builds
- Switch to self-contained .Net binaries to avoid dependency on host runtime
  package
- NationalPACSAccessionNumber is now allowed to be null in all messages

### Dependencies

- Bump HIC.RDMP.Plugin from 4.1.5 to 4.1.6
- Bump MongoDB.Driver from 2.10.4 to 2.11.0
- Bump System.IO.Abstractions from 12.0.10 to 12.1.1
- Bump System.IO.Abstractions.TestingHelpers from 12.0.10 to 12.1.1
- Bump jackson-dataformat-yaml from 2.11.1 to 2.11.2

## [1.10.0] - 2020-07-31

### Changed

- Updated the extraction report to be more human-readable #320, #328
- Add CLI option to CohortPackager to allow an existing report to be recreated
  #321
- Added a runsettings file for NUnit to allow configuration of test output.
  Fixes an issue with TravisCI and NUnit3TestAdapter v3.17.0, which caused the
  test output to spill to over 20k lines.

### Dependencies

- Bump HIC.FAnsiSql from 0.11.1 to 1.0.2
- Bump HIC.RDMP.Dicom from 2.1.5 to 2.1.6
- Bump HIC.RDMP.Plugin from 4.1.3 to 4.1.5
- Bump Magick.NET-Q16-AnyCPU from 7.20.0 to 7.21.1
- Bump Microsoft.Extensions.Caching.Memory from 3.1.5 to 3.1.6
- Bump System.IO.Abstractions from 12.0.1 to 12.0.10
- Bump System.IO.Abstractions from 12.0.1 to 12.0.2
- Bump System.IO.Abstractions.TestingHelpers from 12.0.1 to 12.0.2
- Bump com.fasterxml.jackson.dataformat.jackson-dataformat-yaml from 2.11.0 to
  2.11.1
- Bump org.mockito.mockito-core from 3.3.3 to 3.4.6

## [1.9.0] - 2020-06-22

### Added

- Added image extraction blacklist rejector.
  - Configure with `Blacklists` option (specify a list of Catalogue IDs)
  - Catalogues listed must include one or more column(s) StudyInstanceUID,
    SeriesInstanceUID, SOPInstanceUID.
  - Records in the referenced table will blacklist where any UID is found
    (StudyInstanceUID, SeriesInstanceUID or SOPInstanceUID). This allows
    blacklisting an entire study or only specific images.
  - [breaking] Config on live system may need updated
- Change the extraction directory generation to be
  `<projname>/image-requests/<extractname>`. Fixes
  [MVP Service #159](https://dev.azure.com/smiops/MVP%20Service/_workitems/edit/159/)

### Fixed

- Fixed IsIdentifiable rule order being the order the files are detected in
  rules directory (Now goes IgnoreRules=>ReportRules=>SocketRules)
- Adjust log handling in CTP anonymiser to use SMIlogging setup
- IsIdentifiable case-sensitive rules now implemented with property
- Bugfix for fo-dicom image handling race condition in Release mode builds
  (issue #238)

### Changed

- Refactored `WhiteListRule` to inherit from `IsIdentifiableRule` (affects
  serialization).
  - Parent property `As` replaces `IfClassification`
  - `CaseSensitive` replaces `IfPatternCaseSensitive` and
    `IfPartPatternCaseSensitive` (Also fixes serialization bug)
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

- Fix null check bug in CohortPackager when no files match the extraction
  filter

## [1.8.0] - 2020-04-16

### Added

- Added Terminal.Gui at version 0.81.0
- Added data/IsIdentifiableRules

### Changed

- [Breaking] Promote the PT modality to its own collection in MongoDB
- [Breaking] Renamed `RedisHost` to `RedisConnectionString` in the config
  options for clarity
- Update to .Net Core 3.1 (supported until Dec 2022) since 2.2 support ended
  last year
- Switch CohortExtractor to use batched message producers
- Simplify the Travis build script
- Fail any integration tests in CI if a required service is not available
  (instead of skipping)
- Specified LangVersion 8.0 in all project files
- Upgraded CommandLineParser from 2.5.0 to 2.7.82
- Upgraded CsvHelper from 12.1.2 to 15.0.4
- Upgraded HIC.Rdmp.Dicom from 2.0.8 to 2.0.9
- Upgraded JetBrains.Annotations from 2019.1.3 to 2020.1.0
- Upgraded Magick.NET-Q16-AnyCPU from 7.15.1 to 7.16.0
- Upgraded Microsoft.CodeAnalysis.CSharp.Scripting from 3.5.0-beta2-final to
  3.5.0
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

  - Consume messages from CTP (failed anonymisation) and IsIdentifiable
    (verification)
  - Add support for extraction by modality
  - Remove the final check for the anonymised file. IsIdentifiable handles
    this already
  - Refactor tests

- Start to refactor core RabbitMqAdapter code to allow unit testing

## [1.5.2] - 2020-03-12

### Added

- IsIdentifiableReviewer considers rule capture groups when performing
  redactions (e.g. can now handle custom rules like `^(Ninewells)`)
- IsIdentifiableReviewer adds comment with time/user to rules file e.g.
  `#TZNind - 3/10/2020 1:17:17 PM`
- IsIdentifiableReviewer checks custom patterns match the original Failure
- IsIdentifiable microservice was started with --service but can now be
  started with the service verb allowing it to take additional options. It
  should now be started with `service -y file.yaml`
- IsIdentifiable no longer reads Rules.yaml from the current directory. It now
  has a command line option --RulesDirectory, to go with the already existing
  --RulesFile. That will read all \*.yaml files in the given directory.
  However when run as a microservice the yaml file specifies a DataDirectory;
  the RulesDirectory will implicitly be a subdirectory called
  IsIdentifiableRules from which all \*.yaml files will be read.

### Changed

- IsIdentifiableReviewer now tries to isolate 'Problem Words' when generating
  it's suggested Updater Regex rules (e.g. now suggests `^Ninewells` instead
  of `^Ninewells\ Spike\ CT$`.)

## [1.5.1] - 2020-03-06

- Improved usability of IsIdentifiableReviewer

## [1.5.0] - 2020-03-05

- [Breaking] Updated RabbitMQ extraction config to match extraction plan v2
- Refactor Java exception handling and use of threads
- `TessDirectory` option in [IsIdentifiable] now expects tesseract models file
  to exist (no longer downloads it on demand)
- Added support for outsourcing classification (e.g. NLP) to other processes
  via TCP (entered in [SocketRules] in `Rules.yaml`)
- IsIdentifiable NLP text classification now outsourced via TCP to any
  services configured in
  - StanfordNER implementation written in Java
- New CohortExtractor yaml config option `ProjectPathResolverType` which
  determines the folder structure for extracted images
- Added a script to verify RabbitMQ config files
- Added `DynamicRejector` which takes its cohort extraction rules from a
  script file (of CSharp code)
- Added new application for reviewing IsIdentifiable output files

### Fixed

- Corrected the GetHashCode implementation in the MessageHeader class

## [1.4.5] - 2020-02-26

- Add clean shutdown hook for IdentifierMapper to clean up the worker threads

## [1.4.4] - 2020-02-25

- Update Travis config and Java library install shell script to resolve some
  Travis stability issues
- Adjust batching so workers queue replies/acks while a worker thread commits
  those asynchronously, allowing elastic batch sizes (qosprefetch setting now
  controls maximum batch size, parallelism capped at 50)

## [1.4.3] - 2020-02-21

### Changed

- Batch up RabbitMQ messages/acks in IdentifierMapper to avoid contention with
  the message publishing persistence

## [1.4.2] - 2020-02-18

### Added

- Added unit test for AccessionDirectoryLister as part of
  DicomDirectoryProcessor tests

### Changed

- Make performance counters in RedisSwapper atomic for thread-safety
- Clean up threads when using threaded mode in RabbitMQAdapter
- Use explicit threads rather than Task queueing in IdentifierMapper

## [1.4.1] - 2020-02-17

### Added

- Added randomisation in the retry delay on DicomRelationalMapper (and set
  minimum wait duration to 10s)

### Fixed

- Fixed DLE Payload state being wrong when retrying batches (when it is half /
  completely consumed)
- Added lock on producer sending messages in IdentifierMapper

## [1.4.0] - 2020-02-14

### Added

- Added in memory caching of the last 1024 values when using Redis wrapper for
  an IdentifierSwapper
- Added some parallelism and marshalling of backend queries to improve
  throughput in IdentifierSwapper
- Added temporary flag for RabbitMQAdapter parallelism for the above. Only
  enabled for the IdentifierMapper for now
- Added new mode to DicomDirectoryProcessor which allows reading in a list of
  accession directories

## [1.3.1] - 2020-02-13

### Changed

- Pinned fo-dicom to v4.0.1

## [1.3.0] - 2020-02-06

### Added

- Added (optional) DicomFileSize property to ETL pipeline. Add to template(s)
  with:

```yaml
- ColumnName: DicomFileSize
  AllowNulls: true
  Type:
      CSharpType: System.Int64
```

- Added new microservice IsIdentifiable which scans for personally
  identifiable information (in databases and dicom files)
- Added support for custom rules in IsIdentifiable (entered in `Rules.yaml`)
  - Rules are applied in the order they appear in this file
  - Rules are applied before any other classifiers (i.e. to allow
    whitelisting rules)
- Added `RedisSwapper` which caches answers from any other swapper. Set
  `RedisHost` option in yaml to use.

### Changed

- Updated RDMP and Dicom plugins
- Refactor Java exception handling and use of threads

## [1.2.3] - 2020-01-09

### Changed

- RabbitMQAdapter: Improve handling of timeouts on connection startup

### Added

- Improved logging in IdentifierSwappers

### Changed

- Guid swapper no longer limits input identifiers to a maximum of 10
  characters

### Fixed

- Fixed DicomRelationalMapper not cleaning up STAGING table remnants from
  previously failed loads (leading to crash)

## [1.2.2] - 2020-01-08

### Fixed

- RAW to STAGING migration now lists columns explicitly (previously used
  `SELECT *` which could cause problems if RAW and STAGING column orders
  somehow differed)

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
  - Now :
    `BasicReturn for Exchange 'TEST.IdentifiableImageExchange' Routing Key 'reprocessed' ReplyCode '312' (NO_ROUTE)`
- Added new swapper `TableLookupWithGuidFallbackSwapper` which performs lookup
  substitutions but allocates guids for lookup misses
- Added Travis CI build & deploy for all services

### Changed

- Make exceptions on startup clearer
- Updated to latest RDMP API (4.0.1)
- `TableLookupSwapper` now throws consistent error if the provided table does
  not exist during `Setup` (previously it would error with DBMS specific error
  message at lookup time)

### Fixed

- Fixed freeze condition when exchanges are not mapped to queues
- IdentifierMapper now loads all FAnsi database implementations up front on
  startup

## [1.1.0] - 2019-11-22

### Added

- Improvements to unit and integration tests
- Documentation fixes
- Config file for Dependabot
- Test for DicomFile SkipLargeTags option. Closes
  [#19](https://dev.azure.com/SmiOps/MVP%20Service/_workitems/edit/19)

### Changed

### C# dependencies

- Bumped HIC.DicomTypeTranslation from 1.0.0.3 to 2.1.2
- Bumped HIC.RDMP.Plugin from 3.1.1 to 4.0.1-rc2
- Bumped Newtonsoft.Json from 12.0.2 to 12.0.3
- Bumped RabbitMQ.Client from 5.1.0 to 5.1.2
- Bumped System.IO.Abstractions from 4.2.17 to 7.0.7
- Bumped MongoDB.Driver from 2.8.0 to 2.9.3

### Java dependencies

- Bumped jackson-databind from 2.9.6 to 2.9.10.0

## [1.0.0] - 2019-11-18

First stable release after importing the repository from the private
[SMIPlugin](https://github.com/HicServices/SMIPlugin) repo.

### Added

- ForGuidIdentifierSwapper automatically creates it's mapping database if it
  does not exist on the server referenced (previously only table was
  automatically created)

### Changed

- Updated to
  [Rdmp.Dicom 2.0.2](https://github.com/HicServices/RdmpDicom/blob/main/CHANGELOG.md#202-2019-11-13)
- Updated to
  [Rdmp.Core 3.2.1](https://github.com/HicServices/RDMP/blob/develop/CHANGELOG.md#321---2019-10-30)

### Removed

- Anonymous `MappingTableName` must now be fully specified to pass validation
  (e.g. `mydb.mytbl`). Previously skipping database portion was supported.

[1.0.0]: https://github.com/SMI/SmiServices/releases/tag/1.0.0
[1.1.0]: https://github.com/SMI/SmiServices/compare/1.0.0...1.1.0
[1.10.0]: https://github.com/SMI/SmiServices/compare/v1.9.0...v1.10.0
[1.11.0]: https://github.com/SMI/SmiServices/compare/v1.10.0...v1.11.0
[1.11.1]: https://github.com/SMI/SmiServices/compare/v1.11.0...v1.11.1
[1.12.0]: https://github.com/SMI/SmiServices/compare/v1.11.1...v1.12.0
[1.12.1]: https://github.com/SMI/SmiServices/compare/v1.12.0...v1.12.1
[1.12.2]: https://github.com/SMI/SmiServices/compare/v1.12.1...v1.12.2
[1.13.0]: https://github.com/SMI/SmiServices/compare/v1.12.2...v1.13.0
[1.14.0]: https://github.com/SMI/SmiServices/compare/v1.13.0...v1.14.0
[1.14.1]: https://github.com/SMI/SmiServices/compare/v1.14.0...v1.14.1
[1.15.0]: https://github.com/SMI/SmiServices/compare/v1.14.1...v1.15.0
[1.15.1]: https://github.com/SMI/SmiServices/compare/v1.15.0...v1.15.1
[1.2.0]: https://github.com/SMI/SmiServices/compare/1.1.0...1.2.0
[1.2.1]: https://github.com/SMI/SmiServices/compare/1.2.0...v1.2.1
[1.2.2]: https://github.com/SMI/SmiServices/compare/v1.2.1...v1.2.2
[1.2.3]: https://github.com/SMI/SmiServices/compare/v1.2.2...v1.2.3
[1.3.0]: https://github.com/SMI/SmiServices/compare/v1.2.3...v1.3.0
[1.3.1]: https://github.com/SMI/SmiServices/compare/v1.3.0...v1.3.1
[1.4.0]: https://github.com/SMI/SmiServices/compare/v1.3.1...v1.4.0
[1.4.1]: https://github.com/SMI/SmiServices/compare/v1.4.0...v1.4.1
[1.4.2]: https://github.com/SMI/SmiServices/compare/v1.4.1...v1.4.2
[1.4.3]: https://github.com/SMI/SmiServices/compare/v1.4.2...v1.4.3
[1.4.4]: https://github.com/SMI/SmiServices/compare/v1.4.3...v1.4.4
[1.4.5]: https://github.com/SMI/SmiServices/compare/v1.4.4...v1.4.5
[1.5.0]: https://github.com/SMI/SmiServices/compare/v1.4.5...v1.5.0
[1.5.1]: https://github.com/SMI/SmiServices/compare/v1.5.0...v1.5.1
[1.5.2]: https://github.com/SMI/SmiServices/compare/v1.5.1...v1.5.2
[1.6.0]: https://github.com/SMI/SmiServices/compare/v1.5.2...v1.6.0
[1.7.0]: https://github.com/SMI/SmiServices/compare/v1.6.0...v1.7.0
[1.8.0]: https://github.com/SMI/SmiServices/compare/v1.7.0...v1.8.0
[1.8.1]: https://github.com/SMI/SmiServices/compare/v1.8.0...v1.8.1
[1.9.0]: https://github.com/SMI/SmiServices/compare/v1.8.1...v1.9.0
[2.0.0]: https://github.com/SMI/SmiServices/compare/v1.15.1...v2.0.0
[2.1.0]: https://github.com/SMI/SmiServices/compare/v2.0.0...v2.1.0
[2.1.1]: https://github.com/SMI/SmiServices/compare/v2.1.0...v2.1.1
[3.0.0]: https://github.com/SMI/SmiServices/compare/v2.1.1...v3.0.0
[3.0.1]: https://github.com/SMI/SmiServices/compare/v3.0.0...v3.0.1
[3.0.2]: https://github.com/SMI/SmiServices/compare/v3.0.1...v3.0.2
[3.1.0]: https://github.com/SMI/SmiServices/compare/v3.0.2...v3.1.0
[3.2.0]: https://github.com/SMI/SmiServices/compare/v3.1.0...v3.2.0
[3.2.1]: https://github.com/SMI/SmiServices/compare/v3.2.0...v3.2.1
[4.0.0]: https://github.com/SMI/SmiServices/compare/v3.2.1...v4.0.0
[5.0.0]: https://github.com/SMI/SmiServices/compare/v4.0.0...v5.0.0
[5.0.1]: https://github.com/SMI/SmiServices/compare/v5.0.0...v5.0.1
[5.1.0]: https://github.com/SMI/SmiServices/compare/v5.0.1...v5.1.0
[5.1.1]: https://github.com/SMI/SmiServices/compare/v5.1.0...v5.1.1
[5.1.2]: https://github.com/SMI/SmiServices/compare/v5.1.1...v5.1.2
[5.1.3]: https://github.com/SMI/SmiServices/compare/v5.1.2...v5.1.3
[5.10.0]: https://github.com/SMI/SmiServices/compare/v5.9.0...v5.10.0
[5.10.1]: https://github.com/SMI/SmiServices/compare/v5.10.0...v5.10.1
[5.10.2]: https://github.com/SMI/SmiServices/compare/v5.10.1...v5.10.2
[5.10.3]: https://github.com/SMI/SmiServices/compare/v5.10.2...v5.10.3
[5.2.0]: https://github.com/SMI/SmiServices/compare/v5.1.3...v5.2.0
[5.3.0]: https://github.com/SMI/SmiServices/compare/v5.2.0...v5.3.0
[5.4.0]: https://github.com/SMI/SmiServices/compare/v5.3.0...v5.4.0
[5.5.0]: https://github.com/SMI/SmiServices/compare/v5.4.0...v5.5.0
[5.6.0]: https://github.com/SMI/SmiServices/compare/v5.5.0...v5.6.0
[5.6.1]: https://github.com/SMI/SmiServices/compare/v5.6.0...v5.6.1
[5.7.0]: https://github.com/SMI/SmiServices/compare/v5.6.1...v5.7.0
[5.7.1]: https://github.com/SMI/SmiServices/compare/v5.7.0...v5.7.1
[5.7.2]: https://github.com/SMI/SmiServices/compare/v5.7.1...v5.7.2
[5.8.0]: https://github.com/SMI/SmiServices/compare/v5.7.2...v5.8.0
[5.9.0]: https://github.com/SMI/SmiServices/compare/v5.8.0...v5.9.0
[6.0.0]: https://github.com/SMI/SmiServices/compare/v5.10.3...v6.0.0
[6.1.0]: https://github.com/SMI/SmiServices/compare/v6.0.0...v6.1.0
[unreleased]: https://github.com/SMI/SmiServices/compare/v6.1.0...main
