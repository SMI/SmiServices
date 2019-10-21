
# Cohort Extractor

Primary Author: [Thomas](https://github.com/tznind)

## Contents
 1. [Overview](#1-overview)
 2. [Setup / Installation](#2-setup--installation)
 3. [Exchange and Queue Settings](#3-exchange-and-queue-settings)
 4. [Config](#4-config)
 5. [Expectations](#5-expectations)
 6. [Class Diagram](#6-class-diagram)

### 1. Overview
This service services `ExtractionRequestMessage` which is a request to extract a given set images identified by a key tag (e.g. `SeriesInstanceUID`) collection (e.g. 5000 SeriesInstanceUID values).  It is the job of the Cohort Extractor to identify the images which correspond to the specified key values requested (e.g. the `SeriesInstanceUID`) and generate output messages to downstream processes responsible for anonymising the images.

There can be mulitple datasets in which matching images should be sourced e.g. MR / CT which could even reside on different servers.  Datasets are identified and distinguished from one another through RDMP `ICatalogue` which exists already as part of the data load process (See `DicomRelationalMapper`). 

### 2. Setup / Installation
- Clone the project and build. Any NuGet dependencies should be automatically downloaded
- Edit the yaml.default with the configuration for your environment
- Pick an implementation of `IAuditExtractions` e.g. `Microservices.CohortExtractor.Audit.NullAuditExtractions` and enter the full Type name into default.yaml `AuditorType`
- Pick an implementation of `IExtractionRequestFulfiller` e.g. `Microservices.CohortExtractor.Execution.RequestFulfillers.FromCataloguesExtractionRequestFulfiller` and enter the full Type  name into default.yaml `RequestFulfillerType`.
- Specify the mapping RDMP catalogue database
- Optionally specify a list of Catalogue IDs in CataloguesToExtractFrom (or set it to * to use any).  Depending on your `IExtractionRequestFulfiller` this value might be ignored.

### 3. Exchange and Queue Settings

| Read/Write | Type | Config setting |
| ------------- | ------------- |------------- |
| Read | ExtractionRequestMessage | CohortExtractorOptions.QueueName |
| Write | ExtractFileMessage| CohortExtractorOptions.ExtractFilesProducerOptions |
| Write | ExtractFileCollectionInfoMessage| CohortExtractorOptions.ExtractFilesInfoProducerOptions |


| Command Line Options | Purpose |
| ------------- | ------------- |
|CliOptions | Allows overriding of which yaml file is loaded. |

### 4. Config
| YAML Section  | Purpose |
| ------------- | ------------- |
| RabbitOptions | Describes the location of the rabbit server for sending messages to |
| RDMPOptions | Describes the location of the Microsoft Sql Server RDMP platform databases which keep track of load configurations, available datasets to extract images from (tables) etc |
| CohortExtractorOptions | Which Catalogues to extract, which classes to instantiate to do the extraction |

| Command Line Options | Purpose |
| ------------- | ------------- |
|CliOptions | Allows overriding of which yaml file is loaded. |

### 5. Expectations

All matching of request criteria is handled by `IExtractionRequestFulfiller`.

All audit is handled by `IAuditExtractions`.

The extraction destination is handled by `IProjectPathResolver`

#### Data Failure States

- No files matching a given tag X
	- ???
- No value in patient id substitution Y
	- ???
- Others? TODO


#### Environmental Failure States

 - Operation on loss of RabbitMQ connection:
	- No special logic
- Operation on loss of access to catalogues:
	- Any Exception thrown by the `ISwapIdentifiers` will not be caught triggering a Fatal on the `Consumer`.

	
### 6. Class Diagram
![Class Diagram](./Images/ClassDiagram.png)
