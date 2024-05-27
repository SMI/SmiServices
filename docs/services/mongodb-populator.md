# MongoDB Populator

Primary Author: [Ruairidh](https://github.com/rkm)

## Contents

1.  [Overview](#1-overview)
2.  [Setup / Installation](#2-setup--installation)
3.  [Exchange and Queue Settings](#3-exchange-and-queue-settings)
4.  [Config](#4-config)
5.  [Expectations](#5-expectations)
6.  [Class Diagram](#6-class-diagram)

### 1. Overview

Stores the Dicom Tag data in `DicomFileMessage` and/or `SeriesMessage` into a MongoDB database document store.

### 2. Setup / Installation

-   Install [MongoDB](https://docs.mongodb.com/manual/installation/), steps will be specific to your environment
-   Optional: Install a GUI such as [Compass](https://www.mongodb.com/products/compass) to easily work with MongoDB
-   Clone the project and build. Any NuGet dependencies should be automatically downloaded
-   Edit the yaml.default with the configuration for your environment
-   Ensure MongoDB is running and run MongoDBPopulator.exe from a commandline

### 3. Exchange and Queue Settings

| Read/Write | Type             | Config setting                                               |
| ---------- | ---------------- | ------------------------------------------------------------ |
| Read       | DicomFileMessage | MongoDbPopulatorOptions.ImageQueueConsumerOptions.QueueName  |
| Read       | SeriesMessage    | MongoDbPopulatorOptions.SeriesQueueConsumerOptions.QueueName |

### 4. Config

| YAML Section            | Purpose                                                                           |
| ----------------------- | --------------------------------------------------------------------------------- |
| RabbitOptions           | Describes the location of the rabbit server for sending messages to               |
| MongoDatabases          | Contains the connection strings and database names to write tag data to           |
| MongoDbPopulatorOptions | Queue names to read messages from, error threshold and how often to push to mongo |

| Command Line Options | Purpose                                         |
| -------------------- | ----------------------------------------------- |
| CliOptions           | Allows overriding of which yaml file is loaded. |

### 5. Expectations

Errors are [logged as normal for a MicroserviceHost](../../common/Smi.Common/README.md#logging)

#### Data Failure States

-   Operation on receiving corrupt message.
    -   Program will send Nack with `requeue` flag set to `false`. This will indicate to RabbitMQ to send the message to the dead letter exchange for analysis.

#### Environmental Failure States

-   Operation on loss of MongoDB connection:
    -   Program will attempt to reconnect and send a warning message every \<x\> minutes until the connection is recovered. If no connection found after \<y\> minutes, it will Nack all messages it has received and enter a paused state, sending a `fatal` error or similar.

### 6. Class Diagram

![Class Diagram](./Images/ClassDiagram.png)
