
# Cohort Extractor

Primary Author: [Thomas](https://github.com/tznind)

## Contents
 1. [Overview](#1-overview)
 2. [Setup / Installation](#2-setup--installation)
 3. [Exchange and Queue Settings](#3-exchange-and-queue-settings)
 4. [Config](#4-config)
    - [Fulfiller]
    - [Rejector]
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

#### Fulfiller

The set of images that __could__ be extracted is controlled by the `IExtractionRequestFulfiller`.  

The current recommended implementation is [FromCataloguesExtractionRequestFulfiller].  This fulfiller will look up one or more tables or multi table joins (Catalogues) and search for the provided extraction key (e.g. SeriesInstanceUID = x)

The matched records are what will be reported on e.g. "for x UIDs we found y available images".  From this result set a subset will be rejected (because you have made row level decisions not to extract particular images).  This is handled by the [Rejector]

Configure the fulfiller in your options yaml:

```yaml
OnlyCatalogues: 1,2,3
RequestFulfillerType: Microservices.CohortExtractor.Execution.RequestFulfillers.FromCataloguesExtractionRequestFulfiller 
```

#### Rejector

Records matched by the [Fulfiller] are passed to the `IRejector` (if any is configured).  This class can make last minute decisions on a row by row level to either extract or forbid (with a specific provided reason) the extraction of an image.

The currently recommended implementation is [DynamicRejector]. To use the dynamic rejector edit your options yaml as follows:

```yaml
RejectorType: Microservices.CohortExtractor.Execution.RequestFulfillers.Dynamic.DynamicRejector
```

Using the [DynamicRejector] also requires you to configure a file [DynamicRules.txt] in the execution directory of your binary.  An example is provided (see [DynamicRules.txt]).

Rules are written in C# and can only index fields that appear in the records returned by the [Fulfiller].

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

[Rejector]: #rejector
[Fulfiller]: #fulfiller
[DynamicRules.txt]: ./DynamicRules.txt
[DynamicRejector]: ./Execution/RequestFulfillers/Dynamic/DynamicRejector.cs
[FromCataloguesExtractionRequestFulfiller]: ./Execution/RequestFulfillers/FromCataloguesExtractionRequestFulfiller.cs