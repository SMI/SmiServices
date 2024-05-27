# DicomReprocessor

Primary Author: [Ruairidh MacLeod](https://github.com/rkm)

## Contents

1.  [Overview](#1-overview)
2.  [Setup / Installation](#2-setup--installation)
3.  [Config](#4-config)
4.  [Queue Settings](#3-queue-settings)
5.  [Expectations](#5-expectations)
6.  [Class Diagram](#6-class-diagram)

### 1. Overview

Pulls documents from a MongoDB collection and republishes them to RabbitMQ as DicomFileMessages.

In future this will have to option to run in 'TagPromotion' mode, where only selected tags are republished.

### 2. Setup / Installation

-   Clone the project and build. Any NuGet dependencies should be automatically downloaded
-   Setup a yaml file with the configuration for your environment
-   Run the normal load pipeline so that your MongoDb has some records in it
-   Run `dotnet DicomReprocessor.dll -y <config> -c <collection> [options]`

### 3. Config

Requires the following fields from the default YAML config:

-   `RabbitOptions`
-   `MongoDatabases.DicomStoreOptions`
-   `DicomReprocessorOptions`

### 4. Exchanges and Queues

Consumes messages from:

-   `None`

Writes messages to:

-   `DicomReprocessorOptions.ReprocessingProducerOptions`

### 5. Expectations

Errors are [logged as normal for a MicroserviceHost](../../common/Smi.Common/README.md#logging)

### 6. Class Diagram

![Class Diagram](./Images/ClassDiagram.png)
