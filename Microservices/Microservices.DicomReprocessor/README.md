# DicomReprocessor

Primary Author: [Ruairidh MacLeod](https://github.com/Ruairidh)

## Contents
 1. [Overview](#1-overview)
 2. [Setup / Installation](#2-setup--installation)
 3. [Queue Settings](#3-queue-settings)
 4. [Config](#4-config)
 5. [Expectations](#5-expectations)
 6. [Class Diagram](#6-class-diagram)

### 1. Overview
Pulls documents from a MongoDB collection and republishes them as DicomFileMessages for (re)loading by DicomRelationalMapper.

In future, this will have the ability to both take a query to run on MongoDB, and run in a 'TagPromotion' mode (only selected tags are reprocessed).

### 2. Setup / Installation
- Clone the project and build. Any NuGet dependencies should be automatically downloaded
- Setup a yaml file with the configuration for your environment
- Run the normal load pipeline so that your MongoDb has some records in it
- Run `DicomReprocessor.exe` with your yaml config

### 3. Exchange and Queue Settings

| Read/Write | Type | Config setting |
| ------------- | ------------- |------------- |
| Write | DicomFileMessage | `DicomReprocessorOptions.IdentImageProducerOptions` |

### 4. Config
| YAML Section  | Purpose |
| ------------- | ------------- |
| RabbitOptions | Describes the location of the rabbit server for sending messages to. |
| MongoDatabases | Contains the connection strings and database names to read tag data from |
| DicomReprocessorOptions | Describes the exchanges to run queries on and which queries should be run for image reprocessing/promotion. |


| Command Line Options | Purpose |
| ------------- | ------------- |
|CliOptions | Allows overriding of which yaml file is loaded. |

### 5. Expectations
Errors are [logged as normal for a MicroserviceHost](../Microservices.Common/README.md#logging)

### 6. Class Diagram
![Class Diagram](./Images/ClassDiagram.png)
