
# Cohort Packager

Primary Author: [Ruairidh MacLeod](https://github.com/rkm)

## Contents

 1. [Overview](#1-overview)
 2. [Setup / Installation](#2-setup--installation)
 3. [Queue Settings](#3-queue-settings)
 4. [Config](#4-config)
 5. [Expectations](#5-expectations)

### 1. Overview

Collects all information regarding an extraction job, and monitors the filesystem for the anonymised files. Persists all information to a MongoDB collection.

### 2. Setup / Installation

- Clone the project and build. Any NuGet dependencies should be automatically downloaded
- Setup a yaml file with the configuration for your environment
- Run `CohortPackager.exe` with your yaml config

### 3. Exchange and Queue Settings

| Read/Write | Type | Config setting |
| ------------- | ------------- |------------- |
|Read|ExtractRequestMessage|`DicomReprocessorOptions.ExtractRequestInfoOptions`|
|Read|ExtractRequestInfoMessage|`DicomReprocessorOptions.ExtractFilesInfoOptions`|
|Read|ExtractFileStatusMessage | `DicomReprocessorOptions.AnonImageStatusOptions` |

### 4. Config

| YAML Section  | Purpose |
| ------------- | ------------- |
|JobWatcherTickrate|How often the filesystem is checked for anonymised files (in seconds)|

### 5. Expectations

Errors are [logged as normal for a MicroserviceHost](../Microservices.Common/README.md#logging)
