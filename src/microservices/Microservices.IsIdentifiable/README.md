# IsIdentifiable

Primary Author: [Thomas](https://github.com/tznind)

## Contents
 1. [Overview](#1-overview)
 2. [Setup / Installation](#2-setup--installation)
 3. [Exchange and Queue Settings](#3-exchange-and-queue-settings)
 4. [Config](#4-config)
 5. [Expectations](#5-expectations)
 6. [Class Diagram](#6-class-diagram)

### 1. Overview
This service evaluates 'data' for personally identifiable values (e.g. names).  It can source data from a veriety of places (e.g. databases, file system).

### 2. Setup / Installation

To run IsIdentifiable you must first build the microservice.  Then download the Stanford NER classifier:

```bash
dotnet publish -r win-x64
cd ./bin/AnyCPU/Debug/netcoreapp2.2/win-x64/publish

#Download a classifier e.g. http://nlp.stanford.edu/software/stanford-ner-2016-10-31.zip

```

Once built you can find help on running under each mode e.g.:

```
./IsIdentifiable.exe --help
./IsIdentifiable.exe db --help
./IsIdentifiable.exe dir --help
```

### 3. Exchange and Queue Settings

> TODO: Not yet implemented

### 4. Config

> TODO: 

### 5. Expectations

> TODO: 

#### Data Failure States

> TODO: 

#### Environmental Failure States
 
> TODO: 

### 6. Class Diagram
![Class Diagram](./IsIdentifiable.png)
