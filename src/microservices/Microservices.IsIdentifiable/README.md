# IsIdentifiable

Primary Author: [Thomas](https://github.com/tznind)

## Contents
 1. [Overview](#overview)
 1. [Setup](#setup)
 1. [Rules](#rules) 
    1. [Basic Rules](#basic-rules) 
    2. [Socket Rules](#socket-rules) 
 1. [Exchange and Queue Settings](#exchange-and-queue-settings)
 1. [Expectations](#expectations)
 1. [Class Diagram](#class-diagram)

### Overview
This service evaluates 'data' for personally identifiable values (e.g. names).  It can source data from a veriety of places (e.g. databases, file system).

### Setup

To run IsIdentifiable you must first build the microservice then download the required data models for NER and OCR.

#### Downloads

The following downloads are required to run the software:

| File     | Destination |  Windows Script |  Linux Script  |
|----------|-------------|-------- |------|
| [Tesseract Data files (pixel OCR models)](https://github.com/tesseract-ocr/tessdata/raw/master/eng.traineddata) | `./data/tessdata` |  [download.ps1](../../../data/tessdata/download.ps1)|  [download.sh](../../../data/tessdata/download.sh)|
| [Stanford NER Classifiers](http://nlp.stanford.edu/software/stanford-ner-2016-10-31.zip)*    |  `./data/stanford-ner`     | [download.ps1](../../../data/stanford-ner/download.ps1)  | [download.sh](../../../data/stanford-ner/download.sh) |

_*Required for NERDaemon_
 

```bash
dotnet publish

cd ./src/microservices/Microservices.IsIdentifiable/bin/AnyCPU/Debug/netcoreapp2.2/

#Generic help (lists modes)
dotnet IsIdentifiable.dll --help

#Specific help (for a given mode e.g. 'db')
dotnet ./IsIdentifiable.dll db --help
```

An example command (evaluate all the images in `C:\MassiveImageArchive`) would be as follows:

```
dotnet IsIdentifiable.dll dir -d C:/MassiveImageArchive --storereport
```

The outputs of this (on some anonymised data):

```
Resource,ResourcePrimaryKey,ProblemField,ProblemValue,PartWords,PartClassifications,PartOffsets
C:\MassiveImageArchive\DOI\000001.dcm,1.3.6.1.4.1.9590.100.1.2.64408251011211630124074907290278463475,"(0008,0005)",ISO_IR 100,ISO,Organization,0
C:\MassiveImageArchive\DOI\000001.dcm,1.3.6.1.4.1.9590.100.1.2.64408251011211630124074907290278463475,"(0018,1016)",MathWorks,MathWorks,Organization,0
C:\MassiveImageArchive\DOI\000001.dcm,1.3.6.1.4.1.9590.100.1.2.64408251011211630124074907290278463475,"(0018,1018)",MATLAB,MATLAB,Person,0
C:\MassiveImageArchive\DOI\000001.dcm,1.3.6.1.4.1.9590.100.1.2.64408251011211630124074907290278463475,"(0020,0010)",DDSM,DDSM,Person,0
C:\MassiveImageArchive\DOI\000002.dcm,1.3.6.1.4.1.9590.100.1.2.423893162212842428532864042250901777433,"(0008,0005)",ISO_IR 100,ISO,Organization,0
C:\MassiveImageArchive\DOI\000002.dcm,1.3.6.1.4.1.9590.100.1.2.423893162212842428532864042250901777433,"(0018,1018)",MATLAB,MATLAB,Person,0
C:\MassiveImageArchive\DOI\000002.dcm,1.3.6.1.4.1.9590.100.1.2.423893162212842428532864042250901777433,"(0020,0010)",DDSM,DDSM,Person,0
C:\MassiveImageArchive\DOI\000003.dcm,1.3.6.1.4.1.9590.100.1.2.84709658512632788123980174250729731712,"(0008,0005)",ISO_IR 100,ISO,Organization,0
C:\MassiveImageArchive\DOI\000003.dcm,1.3.6.1.4.1.9590.100.1.2.84709658512632788123980174250729731712,"(0018,1016)",MathWorks,MathWorks,Organization,0
C:\MassiveImageArchive\DOI\000003.dcm,1.3.6.1.4.1.9590.100.1.2.84709658512632788123980174250729731712,"(0018,1018)",MATLAB,MATLAB,Person,0
C:\MassiveImageArchive\DOI\000003.dcm,1.3.6.1.4.1.9590.100.1.2.84709658512632788123980174250729731712,"(0020,0010)",DDSM,DDSM,Person,0
C:\MassiveImageArchive\DOI\Calc-Test_P_00038_LEFT_CC\1.3.6.1.4.1.9590.100.1.2.85935434310203356712688695661986996009\1.3.6.1.4.1.9590.100.1.2.374115997511889073021386151921807063992\000000.dcm,1.3.6.1.4.1.9590.100.1.2.289923739312470966435676008311959891294,"(0008,0005)",ISO_IR 100,ISO,Organization,0
C:\MassiveImageArchive\DOI\Calc-Test_P_00038_LEFT_CC\1.3.6.1.4.1.9590.100.1.2.85935434310203356712688695661986996009\1.3.6.1.4.1.9590.100.1.2.374115997511889073021386151921807063992\000000.dcm,1.3.6.1.4.1.9590.100.1.2.289923739312470966435676008311959891294,"(0018,1016)",MathWorks,MathWorks,Organization,0
C:\MassiveImageArchive\DOI\Calc-Test_P_00038_LEFT_CC\1.3.6.1.4.1.9590.100.1.2.85935434310203356712688695661986996009\1.3.6.1.4.1.9590.100.1.2.374115997511889073021386151921807063992\000000.dcm,1.3.6.1.4.1.9590.100.1.2.289923739312470966435676008311959891294,"(0018,1018)",MATLAB,MATLAB,Person,0
C:\MassiveImageArchive\DOI\Calc-Test_P_00038_LEFT_CC\1.3.6.1.4.1.9590.100.1.2.85935434310203356712688695661986996009\1.3.6.1.4.1.9590.100.1.2.374115997511889073021386151921807063992\000000.dcm,1.3.6.1.4.1.9590.100.1.2.289923739312470966435676008311959891294,"(0020,0010)",DDSM,DDSM,Person,0
[...]
```

You can run pixel data (OCR) by passing the `--tessdirectory` flag:

```
dotnet IsIdentifiable.dll dir -d C:\MassiveImageArchive --storereport --tessdirectory E:/SmiServices/data/tessdata/
```

### Rules

#### Basic Rules

Some rules come out of the box (e.g. CHI/Postcode) but for the rest you must configure rules in Rules.yaml.

There can either result in a value being Reported or Ignored (i.e. not passed to any downstream classifiers).  Rules can apply to all columns (e.g. Ignore the Modality column) or only those values that match a Regex.

```yaml
BasicRules: 
  # Report as an error any values which contain 2 digits
  - IfPattern: "[0-9][0-9]"
    Action: Report
    As: PrivateIdentifier

  # Do not run any classifiers on the Modality column
  - Action: Ignore
    IfColumn: Modality
```

#### Socket Rules

You can outsource the classification to seperate application(s) (e.g. NERDaemon) by adding `Socket Rules`

```yaml
SocketRules:   
  - Host: 127.0.123.123
    Port: 1234
```

The TCP protocol starts with IsIdentifiable sending the word for classification i.e.

```
Sender: word or sentence\0
```

The service is expected to respond with 0 or more classifications of bits in the word that are problematic.  These take the format:

```
Responder: Classification\0Offset\0Offending Word(s)\0
```

Once the responder has decided there are no more offending sections (or there were none to begin with) it sends a double null terminator.  This indicates that the original word or sentence has been fully processed and the Sender can send the next value requiring validation.

```
Responder: \0\0
```

### Exchange and Queue Settings

In order to run as a microservice you should call it with the `--service` flag

| Read/Write | Type | Config setting |
| ------------- | ------------- |------------- |
| Read | ExtractFileMessage | IsIdentifiableOptions.QueueName |
| Write | IsIdentifiableMessage | IsIdentifiableOptions.IsIdentifiableProducerOptions.ExchangeName |

### Config

| YAML Section  | Purpose |
| ------------- | ------------- |
| RabbitOptions | Describes the location of the rabbit server for sending messages to |
| IsIdentifiableOptions | Describes what `IClassifier` to run and where the classifier models are stored |

### Expectations

> TODO: 

#### Data Failure States

> TODO: 

#### Environmental Failure States
 
> TODO: 

### Class Diagram
![Class Diagram](./IsIdentifiable.png)
