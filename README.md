
[![Build Status](https://travis-ci.org/SMI/SmiServices.svg?branch=master)](https://travis-ci.org/SMI/SmiServices)

# SMIPlugin

Scottish Medical Imaging plugin is a suite of microservices written in C# and Java (communicating through [RabbitMQ](https://www.rabbitmq.com/)) and a plugin for [RDMP](https://github.com/HicServices/rdmp).  The software loads Dicom Tag data (extracted from clinical images) into MongoDB and Relational database tables for the purposes of generating anonymous linked research extracts (including image anonymisation).

## Contents

1. [Microservices](#microservices)
	* [Data Load Microservices](#data-load-microservices)
	* [Image Extraction Microservices](#image-extraction-microservices)
2. [Solution Overivew](#solution-overview)
3. [Building](#building)
4. [Testing](#testing)
5. [Package Hierarchy](#package-hierarchy)

## Microservices

All microservices [follow the same design pattern](./Microservices/Smi.Common/README.md).

The following RabbitMQ microservices have been written.  Microservices are loosely coupled, usually reading and writing only a single kind of message.  Each Queue and Exchange as implemented supports only one Type of `Smi.Common.Messages.IMessage`.

Microservices can be configured through it's [Configuration](#configuration-file) file.

Microservices can be controlled through RabbitMQ messages. The currently supported commands and instructions can be found [here](./Microservices/Smi.Common/Messaging/readme.md).

### Data Load Microservices

![loaddiagram](/Documentation/Images/LoadMicroservices.png)

| Microservice / Console App| Description |
| ------------- | ------------- |
| [ProcessDirectory](./Microservices/Microservices.ProcessDirectory/Readme.md)  | Enumerates directories and generates `AccessionDirectoryMessage` for those that contain dicom files.|
| [DicomTagReader](./Microservices/Microservices.DicomTagReader/Readme.md)  | Opens dicom files found in `AccessionDirectoryMessage` directories and converts to JSON as a `DicomFileMessage`.  Also creates a summary record of the whole series as a `SeriesMessage`.|
| [IdentifierMapper](./Microservices/Microservices.IdentifierMapper/Readme.md)  | Replaces the `PatientID` Dicom Tag in a `DicomFileMessage` using a specified mapping table.|
| [MongoDBPopulator](./Microservices/Microservices.MongoDBPopulator/Readme.md)  | Stores the Dicom Tag data in `DicomFileMessage` and/or `SeriesMessage` into a MongoDB database document store. |
| [DicomRelationalMapper](./Microservices/Microservices.DicomRelationalMapper/Readme.md)  | Runs an RDMP data load configuration with a batch of `DicomFileMessage` to load Dicom Tag data into a relational database (MySql or Microsoft Sql Server).|
| [DicomReprocessor](./Microservices/Microservices.DicomReprocessor/Readme.md)  | Runs a MongoDB query on the database populated by `MongoDBPopulator` and converts the results back into `DicomFileMessage` for (re)loading by `DicomRelationalMapper`.|

### Image Extraction Microservices

![extractiondiagram](/Documentation/Images/ExtractionMicroservices.png)

| Microservice / Console App| Description |
| ------------- | ------------- |
| [ExtractorCL](./java/Microservices/Microservices.ExtractorCL/README.md)  | Reads SeriesInstanceUIDs from a CSV file and generates `ExtractionRequestMessage` and audit message `ExtractionRequestInfoMessage`.|
| [CohortExtractor](./Microservices/Microservices.CohortExtractor/Readme.md)  | Looks up SeriesInstanceUIDs in `ExtractionRequestMessage` and does relational database lookup(s) to resolve into physical image file location.  Generates  `ExtractFileMessage` and audit message `ExtractFileCollectionInfoMessage`.|
| [CTPAnonymiser](./java/Microservices/Microservices.CTPAnonymiser/README.md)  | Microservice wrapper for [CTP](https://github.com/johnperry/CTP).  Anonymises images specified in  `ExtractFileMessage` and copies to specified output directory.  Generates audit message `ExtractFileStatusMessage`.|
|[CohortPackager](./Microservices/Microservices.CohortPackager/README.md)  | Records all audit messages and determines when jobs are complete.|
|[DicomRepopulator](./Microservices/Microservices.DicomRepopulator/Readme.md) | Inserts Dicom Tags recorded in a CSV back into (anonymised) images.  This allows Dicom Tag data to be anonymised like regular relational data but still appear in final images given to researchers.|

### Audit and Logging Systems

| Audit System | Description|
| ------------- | ------------- |
| [NLog](http://nlog-project.org/) | All Microservices log all activity to NLog, the manifestation of these logs can be to file/console/server etc as configured in the app.config file.|
| [Message Audit](./Microservices/Smi.Common/README.md#logging) | Every message sent by a microservice has a unique Guid associated with it.  When a message is issued in response to an input message (all but the first message in a chain) the list of legacy message Guids is maintained.  This list is output as part of NLog logging.|
|[Data Load Audit](./Microservices/Microservices.DicomRelationalMapper/Readme.md#7-audit)|The final Message Guid of every file identified for loading is recorded in the relational database image table.  In addition a valid from / data load ID field is recorded and any UPDATEs that take place (e.g. due to reprocessing a file) results in a persistence record being created in a shadow archive table.|
| [Extraction Audit (MongoDB)](./Microservices/Microservices.CohortPackager/README.md) | CohortPackager is responsible for auditing extraction Info messages from all extraction services, recording which images have been requested and when image anonymisation has been completed.  This is currently implemented through `IExtractJobStore`.|
| CohortExtractor Audit | Obsolete interface `IAuditExtractions` previously existed to record the linkage results and patient release identifiers.|
| Fatal Error Logging | All Microservices that crash or log a fatal error are shut down and log a message to the Fatal Error Logging Exchange.  TODO: Nobody listens to this currently.|
| Quarantine | TODO: Doesn't exist yet.|


## Solution Overview

Appart from the Microservices (documented above) the following library classes are also included in the solution:

| Project Name | Path | Description|
| ------------- | ----- | ------------- |
| Dicom File Tester |/Applications| Application for testing DICOM files compatibility with Dicom<->JSON and Dicom to various database type conversions and back. It basically takes a file and pushes it through the various converters to see what breaks |
| Dicom Repopulator |/Applications| [See Microservices](#image-extraction-microservices) |
| Template Builder | /Applications| GUI tool for building modality database schema templates.  Supports viewing and exploring dicom tags in files|
| Smi.MongoDB.Common | /Reusable | Library containing methods for interacting with MongoDb |

## Building

### Building the C# Projects

Building requires the [.NET Core 2.2 SDK](https://dotnet.microsoft.com/download/dotnet-core/2.2) and [Ruby/Rake](https://github.com/ruby/rake). This can then be built with:

```bash
# Non-Windows systems only
> source scripts/linuxBuildSetup.sh

> rake build
```

The rake build can be configured by overriding the environment variables specified in `rakeconfig.rb`.

### Building the Java Projects

Building the Java projects requires Maven. The CTP dependency first needs to be manually installed:

- Linux

```shell
> cd lib/java/
> ./installDat.sh
```

- Windows

```shell
> cd lib\java\
> .\installDat.bat
```

The projects can then be built by returning to the top level directory and running:

```shell
> mvn -f src/common/com.smi.microservices.parent/pom.xml clean install
```

This will compile and run the tests for the projects. The full test suite requires a local RabbitMQ server, however these can be skipped by passing `-PunitTests`. The entire test sutie can be skipped by passing `-DskipTests`.

Note: If you have Maven `>=3.6.1` then you can pass `-ntp` to each of the above commands in order to hide the large volume of messages related to the downloading of dependencies.

### Building Release Packages

To manually build release packages:

```bash
# Non-Windows systems only
> source scripts/linuxBuildSetup.sh

> rake release_local[<os>]
```

Where `<os>` is the [Runtime Identifier](https://docs.microsoft.com/en-us/dotnet/core/rid-catalog) for the target platform, usually `win-x64` or `linux-x64`.

## Developing

Development requires Visual Studio 2017 or later. Simply open the SMIPlugin.sln file.

## Testing

SMI is built using a microservices architecture and is primarily concerned with translating Dicom tag data into database records (in both MongoDb, Sql Server and MySql).  Tests are split into those that:

- RequiresRelationalDb (Microsoft Sql Server / MySql)
- RequiresMongoDb (MongoDb)
- RequiresRabbit (RabbitMQ Server)
- Unit tests

Tests with the respective attributes will only run when these services exist in the test/development environment.  Connection strings/ports for these services can be found in:

- TestDatabases.txt (Relational Databases)
- default.yaml (RabbitMQ / MongoDb)
- Mongo.yaml
- Rabbit.yaml
- RelationalDatabases.yaml

For setting up the RDMP platform databases see https://github.com/HicServices/RDMP/blob/master/Documentation/CodeTutorials/Tests.md

## Package Hierarchy

[HicServices/TypeGuesser](https://github.com/HicServices/TypeGuesser)

[![Build Status](https://travis-ci.org/HicServices/TypeGuesser.svg?branch=master)](https://travis-ci.org/HicServices/TypeGuesser) [![Total alerts](https://img.shields.io/lgtm/alerts/g/HicServices/TypeGuesser.svg?logo=lgtm&logoWidth=18)](https://lgtm.com/projects/g/HicServices/TypeGuesser/alerts/)  [![NuGet Badge](https://buildstats.info/nuget/HIC.TypeGuesser)](https://buildstats.info/nuget/HIC.TypeGuesser)

[HicServices/FAnsiSql](https://github.com/HicServices/FAnsiSql)

[![Build Status](https://travis-ci.org/HicServices/FAnsiSql.svg?branch=master)](https://travis-ci.org/HicServices/FAnsiSql) [![Total alerts](https://img.shields.io/lgtm/alerts/g/HicServices/FAnsiSql.svg?logo=lgtm&logoWidth=18)](https://lgtm.com/projects/g/HicServices/FAnsiSql/alerts/) [![NuGet Badge](https://buildstats.info/nuget/HIC.FAnsiSql)](https://www.nuget.org/packages/HIC.FansiSql/)

[HicServices/DicomTypeTranslation](https://github.com/HicServices/DicomTypeTranslation)

[![Build Status](https://travis-ci.com/HicServices/DicomTypeTranslation.svg?branch=master)](https://travis-ci.com/HicServices/DicomTypeTranslation) [![Total alerts](https://img.shields.io/lgtm/alerts/g/HicServices/DicomTypeTranslation.svg?logo=lgtm&logoWidth=18)](https://lgtm.com/projects/g/HicServices/DicomTypeTranslation/alerts/) [![NuGet Badge](https://buildstats.info/nuget/HIC.DicomTypeTranslation)](https://buildstats.info/nuget/HIC.DicomTypeTranslation)

[HicServices/Rdmp](https://github.com/HicServices/RDMP)

[![Build Status](https://travis-ci.org/HicServices/RDMP.svg?branch=master)](https://travis-ci.org/HicServices/RDMP) [![Total alerts](https://img.shields.io/lgtm/alerts/g/HicServices/RDMP.svg?logo=lgtm&logoWidth=18)](https://lgtm.com/projects/g/HicServices/RDMP/alerts/) [![NuGet Badge](https://buildstats.info/nuget/HIC.RDMP.Plugin)](https://buildstats.info/nuget/HIC.RDMP.Plugin)

[HicServices/Rdmp.Dicom](https://github.com/HicServices/RdmpDicom)

[![Build Status](https://travis-ci.org/HicServices/RdmpDicom.svg?branch=master)](https://travis-ci.org/HicServices/RdmpDicom) [![Total alerts](https://img.shields.io/lgtm/alerts/g/HicServices/RdmpDicom.svg?logo=lgtm&logoWidth=18)](https://lgtm.com/projects/g/HicServices/RdmpDicom/alerts/) [![NuGet Badge](https://buildstats.info/nuget/HIC.RDMP.Dicom)](https://buildstats.info/nuget/HIC.RDMP.Dicom)
