# IsIdentifiable Reviewer

Primary Author: [Thomas](https://github.com/tznind)

## Contents
 1. [Overview](#1-overview)
 2. [Setup / Installation](#2-setup--installation)
 3. [Usage](#usage)
 4. [Unattended Mode](#unattended-mode)

## 1. Overview

This application allows you to rapidly evaluate and act upon the output of the IsIdentifiable tool (reports of suspected Personally Identifiable Information - PII).

![Screenshot](./images/Role.png)
_The review process of potentially PII_

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

![Screenshot](./images/Screenshot1.png)

## Usage

Review the reports and mark either `Ignore` (this is a false positive) or `Update` (this is PII and needs to be redacted).  This will result in a new rule being added to either `NewRules.yaml` (Ignore) or `RedList.yaml` (Update).  Once  a rule is written it will be applied automatically to future reports loaded eliminating the lead to make duplicate decisions.

If you are not in `RulesOnly` mode an SQL UPDATE statement will be issued for the PrimaryKey / Table of the report.

Conceptually these rules are slightly different from the IsIdentifiable rules. IsIdentifiable first uses rules to spot known PII. Then it uses a NLP(NER) tool which attempts to find more PII. Finally it uses whitelist rules to ignore known false positives. Ideally these rules should be fine-tuned to reduce the work of the reviewer so, for example, if the reviewer shows 90% of failures are due to `Manufacturer=AGFA` it would be wise to manually edit IsIdentifiable rules. The Reviewer rules are different in that they are used filter the IsIdentifiable output and either ignore or redact its failure reports. The syntax of the rules files looks similar but is used differently, and has no effect on future runs of IsIdentifiable, only on future Reviews.

The standard view operates on one failure at a time. It shows the full string at the top, with the failures highlighted in green. At the bottom left is the classification of the failure: Person, Organisation, Date, etc. At the bottom right is the column (or DICOM tag) where the failure was found. It is important to check this column because, for example, you should Ignore a hospital name if the column is InstitutionName, but Update it if the column is StudyDescription.

An alternative view available from the `View` menu sorts all of the failures by number of occurences.

The menu `Options | Custom Patterns` menu, when ticked, will provide the opportunity to edit the Ignore/Update rule before it is saved. This allows you to make fine adjustments to the exact pattern which will be redacted. Note that all bracketed patterns are redacted so you can add (or remove) any as necessary. For example, if the full string is `John Smith Hospital^MRI Head^(20/11/2020)` but only the date has been detected you could still redact the hospital name as well by editing the pattern to be `(John Smith Hospital)^.*^\((\d\d/\d\d/\d\d\d\d)\)$` (i.e. adding the name in brackets).

The Custom Patterns window provides several options to edit the pattern:

* `x` - clears currently typed pattern
* `F` - creates a regex pattern that matches the full input value
* `G` - creates a regex pattern that matches only the failing part(s)
* `\d` - replaces all digits with regex wildcards
* `\c` - replaces all characters with regex wildcards
* `\d\c` - replaces all digits and characters with regex wildcards


## Unattended Mode

Once you have a substantial body of rules (or if you have been running in RulesOnly mode) you can apply these to new report files.

This is done by providing as input the report file and an ouptut path (for any remaining reports not covered by the rules):

```
.\IsIdentifiableReviewer -f ./Example.csv -u ./filtered.csv
```
_Make sure that Targets.yaml contains only a single entry and that the server correctly responds (and contains table(s) that match the input file)_

[IsIdentifiable]: ../../microservices/Microservices.IsIdentifiable/README.md