# IsIdentifiable

Primary Author: [Thomas](https://github.com/tznind)

## Contents
 1. [Overview](#overview)
 1. [Setup](#setup)
 1. [Rules](#rules) 
    1. [Basic Rules](#basic-rules) 
    2. [Socket Rules](#socket-rules) 
    3. [White List Rules](#white-list-rules) 
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

The directory must be named `tessdata` and contain a file named `eng.traineddata`

### Rules

Some rules come out of the box (e.g. CHI/Postcode/Date) but for the rest you must configure rules in Rules.yaml.
There are three classes of Rules, BasicRules, SocketRules and WhiteListRules.
They are applied in that order, so if a value is Ignored in a Basic rule it will not be passed to any further rules.
If a value fails in a SocketRule (eg. the NER daemon labels it as a Person), then a subsequent WhiteList rule can Ignore it.

Rules are read from the file `Rules.yaml` but no error is reported if the file does not exist.
Additional rules can be supplied using the `--rulesfile` option.

#### Basic Rules

These can either result in a value being Reported or Ignored (i.e. not passed to any downstream classifiers).  Rules can apply to all columns (e.g. Ignore the Modality column) or only those values that match a Regex.

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

#### White List Rules

The Action for a White List rule must be Ignore because it is intended to allow values previously reported to be ignored as false positives.
All of the constraints must match in order for the rule to Ignore the value.
As soon as a value matches a white list rule no further white list rules are needed.
Unlike a BasicRule whose Pattern matches the full value of a field (column or DICOM tag) the whitelist rule has two Patterns, IfPattern which has the same behaviour and IfPartPattern which matches only the substring that failed. This feature allows context to be specified, see the second example below.
A whitelist rule can also match the failure classification (PrivateIdentifier, Location, Person, Organization, Money, Percent, Date, Time, PixelText, Postcode).
For example, if SIEMENS has been reported as a Person found in the the Manufacturer column,

```yaml
WhiteListRules:
 - Action: Ignore
   IfClassification: Person
   IfColumn: Manufacturer
   IfPartPattern: ^SIEMENS$
```
For example, what seems like a name Brian can be ignored if it occurs in the exact phrase "MR Brian And Skull" using:

```yaml
IfPartPattern: ^Brian$
IfPattern: MR Brian And Skull
```

Note that there is no need to specify ^ and $ in IfPattern as other text before or after it will not change the meaning.

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
| IsIdentifiableOptions | Describes what `IClassifier` to run and where the classifier models are stored. The key `DataDirectory` specifies the path to the data directory. The key `ClassifierType` specifies which classifier to run, typically `Microservices.IsIdentifiable.Service.TesseractStanfordDicomFileClassifier` |

### Expectations

> TODO: 

#### Data Failure States

> TODO: 

#### Environmental Failure States
 
> TODO: 

### Class Diagram
![Class Diagram](./IsIdentifiable.png)
