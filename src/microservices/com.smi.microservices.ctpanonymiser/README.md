# CTPAnonymiser

Primary Author: [Paul Graham] (https://github.com/pjgraham)

## Contents
 1. [Overview](#1-overview)
 2. [Setup / Installation](#2-setup-installation)
 3. [Queue Settings](#3-exchange-and-queue-settings)
 4. [Config](#4-config)
 5. [Expectations](#5-expectations)

### 1. Overview

The CTP Anonymiser app is a RabbitMQ-based tool for anonymising DICOM image data. Launched from the command line, it consumes from a RabbitMQ queue, receiving directory names of DICOM files, anonymises them using the CTP DICOM [Anonymizer](https://mircwiki.rsna.org/index.php?title=The_CTP_DICOM_Anonymizer), and then produces the output anonymised filenames to another RabbitMQ queue. The choice of fields etc to anonymise is determined by a provided anonymisation script file.

### 2. Setup / Installation

The anonymiser is installed via Maven as per the other Java apps, so clone the project from git and run the maven install (see [here](https://github.com/HicServices/SMIPlugin/blob/develop/java/README.md) for details).

### 3. Exchange and Queue Settings

| Read/Write | Type | Config setting |
| ------------- | ------------- |------------- |
| Read| ExtractFileMessage | `CTPAnonymiserOptions.AnonFileConsumerOptions` |
| Write| ExtractFileStatusMessage|`CTPAnonymiserOptions.ExtractFileStatusProducerOptions`|

The ExtractFileMessage contents include the name of a directory of DICOM files to be anonymised. As the files are anonymised, ExtractFileStatusMessage messages are produced indicating success or otherwise.

### 4. Config

As for the other Java and C# microservices, the anonymiser uses the yaml config files documented [here](https://github.com/HicServices/SMIPlugin/blob/develop/Microservices/Microservices.Common/Options/RabbitMqConfigOptions.md).

In particular, the anonymiser uses:

* RabbitOptions - location of the RabbitMQ host etc
* FileSystemOptions - Root directories where files will be discovered/anonymised to
* CTPAnonymiserOptions - the config for the anonymiser consumer and producer

| CLI Options| Switch | Required | Purpose |
| :----- | :-------: |:----: |:---|
|Yaml config|-y| No| Allows overriding of which yaml file to use|
|Anon script|-a| Yes | The anonymisation script file to use |

The anonymisation script is based on the example provided by CTP, but it has been further restricted, for example, to exclude all fields which *may* include user-defined text.

The current anonymisation script can be viewed [here](https://github.com/HicServices/SMIPlugin/tree/develop/Documentation/Anon), file `dicom-whitelist.script`

Note that the CTP also supports pixel anonymisation, but at the moment this is not being exploited.

### 5. Expectations

As each DICOM file is processed, a RabbitMQ status [message](https://github.com/HicServices/SMIPlugin/blob/master/java/Microservices/Microservices.CTPAnonymiser/src/main/java/org/smi/ctpanonymiser/messages/ExtractFileStatusMessage.java) is produced, indicating success or [otherwise](https://github.com/HicServices/SMIPlugin/blob/master/java/Microservices/Microservices.CTPAnonymiser/src/main/java/org/smi/ctpanonymiser/util/ExtractFileStatus.java) of the anonymisation. Unsuccessful anonymisation attempts are tagged to be either retried, or  not retried.

The status message also includes details of the path to the anonymised file, project number, job id etc. 

