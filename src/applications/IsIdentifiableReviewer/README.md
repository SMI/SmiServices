# IsIdentifiable Reviewer

Primary Author: [Thomas](https://github.com/tznind)

## Contents
 1. [Overview](#1-overview)
 2. [Setup / Installation](#2-setup--installation)
 3. [Usage](#usage)
 4. [Unattended Mode](#unattended-mode)

## 1. Overview

This application allows you to rapidly evaluate and act upon the output of the IsIdentifiable tool (reports of suspected Personally Identifiable Information - PII).

You can see the application help by running:

```
.\IsIdentifiableReviewer --help
```

## 2. Setup / Installation

The tool is primarily intended to review [IsIdentifiable] `StoreReport` outputs generated from a relational database.  An [ExampleReport](./ExampleReport.csv) is included in the redistributable.

the first time you run the software you will most likely get the following error:
```
.\IsIdentifiableReviewer

Running Connection Tests
Failed to connect to My Server
Failed to connect to My Other Server
Press any key to launch GUI
```

This is because in addition to reviewing failures the application is designed to update the database to redact strings which you mark as PII.  You can either update `Targets.yaml` to point to the server you are running [IsIdentifiable] on or ignore the error for now.

In the GUI you can open `ExampleReport.csv` and begin marking reports as `Ignore` or `Update`.  If you have not configured `Targets.yaml` then make sure to select the `Rules Only` option.

![Screenshot](./Screenshot1.png)

## Usage

Review the reports and mark either `Ignore` (this is a false positive) or `Update` (this is PII and needs to be redacted).  This will result in a new rule being added to either `NewRules.yaml` (Ignore) or `RedList.yaml` (Update).  Once  a rule is written it will be applied automatically to future reports loaded eliminating the lead to make duplicate decisions.

If you are not in `RulesOnly` mode an SQL UPDATE statement will be issued for the PrimaryKey / Table of the report.

## Unattended Mode

Once you have a substantial body of rules (or if you have been running in RulesOnly mode) you can apply these to new report files.

This is done by providing as input the report file and an ouptut path (for any remaining reports not covered by the rules):

```
.\IsIdentifiableReviewer -f ./Example.csv -u ./filtered.csv
```
_Make sure that Targets.yaml contains only a single entry and that the server correctly responds (and contains table(s) that match the input file)_

[IsIdentifiable]: ../../microservices/Microservices.IsIdentifiable/README.md