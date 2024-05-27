[![main](https://github.com/smi/SmiServices/actions/workflows/main.yml/badge.svg)](https://github.com/smi/SmiServices/actions/workflows/main.yml)
[![CodeQL](https://github.com/SMI/SmiServices/actions/workflows/codeql.yml/badge.svg)](https://github.com/SMI/SmiServices/actions/workflows/codeql.yml)
[![codecov](https://codecov.io/gh/SMI/SmiServices/branch/main/graph/badge.svg?token=O6G26DHLEY)](https://codecov.io/gh/SMI/SmiServices)
![GitHub](https://img.shields.io/github/license/SMI/SmiServices)

# SMI Services

SMI Services is a suite of tools designed to index DICOM image metadata and create de-identified extracts at scale. Communication between services is provided through RabbitMQ.

The following workflows are supported:

-   Reading metadata from DICOM files into various database formats (MongoDB, SQL)
-   Creating anonymous subsets of DICOM files from an archive. This includes tools to scan for residual PII in both DICOM tags and pixel data

## Packages

This repo currently provides 2 packages:

-   `SmiServices` - The suite of services for loading metadata and de-identifying files
-   `CTPAnonymiser` - A service which performs de-identification. A Java runtime is required to launch this

Binaries for each package are available from the [Releases](https://github.com/SMI/SmiServices/releases) page. We support Linux as our primary platform for testing, however development is also supported on both Windows and MacOS.

## Usage

After downloading the required packages, you must configure a RabbitMQ server and the databases. This is currently a self-guided activity, however we are working on automated setup of this in future. For now you can use the following resources:

-   Sample docker-compose files can be found [here](./utils/docker-compose)
-   A basic service config file can be found [here](./data/microserviceConfigs/README.md)
-   RabbitMQ configs for the data load and extraction pipelines can be found [here](./data/rabbitmqConfigs/)

All SmiServices tools are available through the `smi` binary. Run `./smi --help` for a list of tools.

`CTPAnonymiser` is available as an "all-in-one" jar file, which can be run with `java -jar <jarfile>`

## Developing

Python is required in order to use the included convenience scripts for building and testing.

### SmiServices (C#)

A .NET Core SDK matching the version specified [here](global.json) is required.

Visual Studio is not required, but recommended. A `sln` file is provided which can be used to open all projects.

The `./bin/smi` directory contains useful build scripts. You can also build the entire solution manually with `dotnet build`, or individual projects by changing into the appropriate directory, e.g.:

```console
cd src/microservices/Microservices.DicomTagReader
dotnet build
```

### CTPAnonymiser (Java)

Building the Java project requires:

-   A JDK `>= 1.8`
-   Maven `>= 3.6`

The `./bin/ctp` directory contains useful build scripts. You must run `./bin/ctp/installLibs.py` first.

### pre-commit

This repo uses [pre-commit](https://pre-commit.com/) to manage and automatically run a series of linters
and code formatters. After cloning the repo and changing into the directory, run
this once to setup pre-commit.

```console
pip install --upgrade --user pre-commit
pre-commit install
```

Running pre-commit locally is recommended but optional. To remove the hooks from your repo, simply run:

```console
pre-commit uninstall
```
