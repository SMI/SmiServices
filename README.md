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

-   `SmiServices`: The suite of services for image loading and de-identification
-   `CTPAnonymiser`: A java service which performs de-identification

Binaries for each package are available from the [Releases](https://github.com/SMI/SmiServices/releases) page.

## Usage

After downloading the required packages, you must configure RabbitMQ and any required databases

-   Sample docker-compose files for this can be found [here](./utils/docker-compose)
-   A basic service config file can be found [here](./data/microserviceConfigs/README.md)
-   RabbitMQ configs for the data load and extraction pipelines can be found [here](./data/rabbitmqConfigs/)

All SmiServices tools are available through the `smi` binary. Run `./smi --help` for a list of tools.

`CTPAnonymiser` is available as an "all-in-one" jar file.

## Developing

Python is required in order to use the included convenience scripts for building and testing.

### SmiServices (C#)

A .NET Core SDK matching the version specified [here](global.json) is required.

Visual Studio is not required, but recommended. A `sln` file is provided which can be used to open all projects.

`./bin/smi` directory contains useful build scripts. You can also build the entire solution manually with `dotnet build`, or individual projects by changing into the appropriate directory (e.g., `src/microservices/Microservices.DicomTagReader`).

### CTPAnonymiser (Java)

Building the Java project requires:

-   A JDK `>= 1.8`
-   Maven `>= 3.6`

`./bin/ctp` directory contains useful build scripts. You need to run `./bin/ctp/installLibs.py` first.

### pre-commit

This repo uses [pre-commit] to manage and automatically run a series of linters
and code formatters. After cloning the repo and changing into the directory, run
this once to setup pre-commit.

```console
$ pip install pre-commit
$ pre-commit install
```

Running pre-commit locally is optional, since it is also run during any PR. To remove
pre-commit from your repo clone, simply run:

```console
$ pre-commit uninstall
```
