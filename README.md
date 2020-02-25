
[![Build Status](https://travis-ci.com/SMI/SmiServices.svg?branch=master)](https://travis-ci.com/SMI/SmiServices)
![GitHub](https://img.shields.io/github/license/SMI/SmiServices)
[![Total alerts](https://img.shields.io/lgtm/alerts/g/SMI/SmiServices.svg?logo=lgtm&logoWidth=18)](https://lgtm.com/projects/g/SMI/SmiServices/alerts/)

Version: `1.4.4`

# SMI Services

![loaddiagram](./docs/Images/SmiFlow.svg)

A suite of microservices for [loading*](./Glossary.md#loading), anonymising, linking and extracting [large volumnes](#scaleability) of [dicom] medical images to support medical research.

The platform allows [dicom tags] (extracted from clinical images) to be loaded into MongoDB and relational database tables for the purposes of generating anonymous linked research extracts (including image anonymisation).

The latest binaries can be downloaded from the [releases section](https://github.com/SMI/SmiServices/releases/latest).

## Contents

1. [Microservices](#microservices)
   1. [Data Load Microservices](#data-load-microservices)
   2. [Image Extraction Microservices](#image-extraction-microservices)
2. [Solution Overivew](#solution-overview)
3. [Building](#building)
4. [Testing](#testing)
5. [Package Hierarchy](#package-hierarchy)
6. [Scaleability](#scaleability)

## Microservices

All microservices [follow the same design pattern](./src/common/Smi.Common/README.md).

The following  microservices have been written.  Microservices are loosely coupled, usually reading and writing only a single kind of message.  Each Queue and Exchange as implemented supports only one Type of `Smi.Common.Messages.IMessage`.

Microservices can be configured through [the configuration file](./data/microserviceConfigs/default.yaml).

A control queue is provided for controlling Microservices during runtime.  It supports a [limited number of commands](./src/common/Smi.Common/Messaging/readme.md)..

### Data Load Microservices

![loaddiagram](./docs/Images/LoadMicroservices.png)

| Microservice / Console App| Description |
| ------------- | ------------- |
| [ProcessDirectory](./src/applications/Applications.DicomDirectoryProcessor/README.md)  | Enumerates directories and generates `AccessionDirectoryMessage` for those that contain dicom files.|
| [DicomTagReader](./src/microservices/Microservices.DicomTagReader/README.md)  | Opens dicom files found in `AccessionDirectoryMessage` directories and converts to JSON as a `DicomFileMessage`.  Also creates a summary record of the whole series as a `SeriesMessage`.|
| [IdentifierMapper](./src/microservices/Microservices.IdentifierMapper/Readme.md)  | Replaces the `PatientID` Dicom Tag in a `DicomFileMessage` using a specified mapping table.|
| [MongoDBPopulator](./src/microservices/Microservices.MongoDbPopulator/Readme.md)  | Stores the Dicom Tag data in `DicomFileMessage` and/or `SeriesMessage` into a MongoDB database document store. |
| [DicomRelationalMapper](./src/microservices/Microservices.DicomRelationalMapper/Readme.md)  | Runs an RDMP data load configuration with a batch of `DicomFileMessage` to load Dicom Tag data into a relational database (MySql or Microsoft Sql Server).|
| [DicomReprocessor](./src/microservices/Microservices.DicomReprocessor/README.md)  | Runs a MongoDB query on the database populated by `MongoDBPopulator` and converts the results back into `DicomFileMessage` for (re)loading by `DicomRelationalMapper`.|

### Image Extraction Microservices

![extractiondiagram](./docs/Images/ExtractionMicroservices.png)

| Microservice / Console App| Description |
| ------------- | ------------- |
| [ExtractorCL](./src/applications/com.smi.applications.extractorcli/README.md)  | Reads SeriesInstanceUIDs from a CSV file and generates `ExtractionRequestMessage` and audit message `ExtractionRequestInfoMessage`.|
| [CohortExtractor](./src/microservices/Microservices.CohortExtractor/README.md)  | Looks up SeriesInstanceUIDs in `ExtractionRequestMessage` and does relational database lookup(s) to resolve into physical image file location.  Generates  `ExtractFileMessage` and audit message `ExtractFileCollectionInfoMessage`.|
| [CTPAnonymiser](./src/microservices/com.smi.microservices.ctpanonymiser/README.md)  | Microservice wrapper for [CTP](https://github.com/johnperry/CTP).  Anonymises images specified in  `ExtractFileMessage` and copies to specified output directory.  Generates audit message `ExtractFileStatusMessage`.|
| [CohortPackager](./src/microservices/Microservices.CohortPackager/README.md)  | Records all audit messages and determines when jobs are complete.|

### Audit and Logging Systems

| Audit System | Description|
| ------------- | ------------- |
| [NLog](http://nlog-project.org/) | All Microservices log all activity to NLog, the manifestation of these logs can be to file/console/server etc as configured in the app.config file.|
| [Message Audit](./src/common/Smi.Common/README.md#logging) | Every message sent by a microservice has a unique Guid associated with it.  When a message is issued in response to an input message (all but the first message in a chain) the list of legacy message Guids is maintained.  This list is output as part of NLog logging.|
| [Data Load Audit](./src/microservices/Microservices.DicomRelationalMapper/Readme.md#7-audit)|The final Message Guid of every file identified for loading is recorded in the relational database image table.  In addition a valid from / data load ID field is recorded and any UPDATEs that take place (e.g. due to reprocessing a file) results in a persistence record being created in a shadow archive table.|
| [Extraction Audit (MongoDB)](./src/microservices/Microservices.CohortPackager/README.md) | CohortPackager is responsible for auditing extraction Info messages from all extraction services, recording which images have been requested and when image anonymisation has been completed.  This is currently implemented through `IExtractJobStore`.|
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

Building requires the [.NET Core 2.2 SDK](https://dotnet.microsoft.com/download/dotnet-core/2.2)

```bash
$ dotnet build [-r RID]
```

_The RID argument is optional. Use this if you want to build for a different platform e.g. `-r linux-x64` to build for Linux from a Windows machine. See [here](https://docs.microsoft.com/en-us/dotnet/core/rid-catalog) for more info on runtime identifiers._

To build an individual sub-project:

```bash
$ cd src/microservices/Microservices.DicomTagReader/
$ dotnet build
```

This will automatically rebuild any dependent projects which have changes as well.

### Building the Java Projects

Building the Java projects requires Java JDK `>= 1.7` (OpenJDK recommended 🙂), and Maven.

The CTP dependency first needs to be manually installed:

- Linux

```bash
$ cd lib/java/
$ ./installDat.sh
```

- Windows

```bash
$ cd lib\java\
$ .\installDat.bat
```

The projects can then be built and tested by returning to the top level directory and running:

```bash
$ mvn -f src/common/com.smi.microservices.parent/pom.xml clean test
```

This will compile and run the tests for the projects. The full test suite requires a local RabbitMQ server, however these can be skipped by passing `-PunitTests`. The entire test suite can be skipped by instead running `compile`, or by passing `-DskipTests`.

To build a single project and its dependencies, you can do:

```bash
$ mvn -f src/common/com.smi.microservices.parent/pom.xml test -pl com.smi.microservices:extractorcli -am
```

Note: If you have Maven `>=3.6.1` then you can pass `-ntp` to each of the above commands in order to hide the large volume of messages related to the downloading of dependencies.

## Developing

### C# Projects

Development requires Visual Studio 2017 or later. Simply open the SmiServices.sln file.

To run the tests for IsIdentifiable, the Stanford NER classifier is required. This can be downloaded with the included script:

```bash
$ cd data/stanford-ner
$ ./download.sh
```

### Java Projects

Development requires Java JDK `>= 1.7`, and Maven.

## Testing

SMI is built using a microservices architecture and is primarily concerned with translating Dicom tag data into database records (in both MongoDb, Sql Server and MySql). Tests are split into those that:

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

## Note On Versioning

The C# projects share the same release version, which is controlled by the [SharedAssemblyInfo.cs](src/SharedAssemblyInfo.cs) file. The Java projects are versioned independently, set in their pom files, however in practice they follow the release version of the repo overall.

## Scaleability

The services in this repository have been sucessfully used to load all medical imaging data captured in Scotland's National PACS archive.

Scaleability is handled through parallel process execution (using [RabbitMQ]).  This allows slow processes (e.g. reading dicom tags from files on disk) to have more running instances while faster processes have less.  Scalability of large operations (e.g. linkage / cohort identification) is done within the [DBMS] layer.

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

[RabbitMQ]: https://www.rabbitmq.com/
[DBMS]: https://github.com/HicServices/RDMP/blob/develop/Documentation/CodeTutorials/Glossary.md#DBMS
[Dicom]: ./Glossary.md#dicom
[Dicom tags]: ./Glossary.md#dicom-tags

