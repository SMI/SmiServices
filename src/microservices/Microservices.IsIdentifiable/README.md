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

To run IsIdentifiable you must first build the microservice then download the required data models for NER and OCR.

#### Downloads

The following downloads are required to run the software:

| File     | Destination |  Windows Script |  Linux Script  |
|----------|-------------|-------- |------|
|  [Stanford NER Classifiers](http://nlp.stanford.edu/software/stanford-ner-2016-10-31.zip)    |  `./data/stanford-ner`     | [download.ps1](../../../data/stanford-ner/download.ps1)  | [download.sh](../../../data/stanford-ner/download.sh) |
|  [Dotnet Core compatible IKVM binaries](https://codeload.github.com/ams-ts-ikvm/ikvm-bin/zip/net_core_compat)    |  bin directory*     |   |  |
| [Tesseract Data files (pixel OCR models)](https://github.com/tesseract-ocr/tessdata/raw/master/eng.traineddata) | `./data/tessdata` |  [download.ps1](../../../data/tessdata/download.ps1)|

 *e.g. `./src/microservices/Microservices.IsIdentifiable/bin/AnyCPU/Debug/netcoreapp2.2`
 

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
dotnet IsIdentifiable.dll dir -c E:/SmiServices/data/stanford-ner/stanford-ner-2016-10-31/classifiers/english.all.3class.distsim.crf.ser.gz -d C:/MassiveImageArchive --storereport
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
dotnet IsIdentifiable.dll dir -c E:\SmiServices\data\stanford-ner\stanford-ner-2016-10-31/classifiers/english.all.3class.distsim.crf.ser.gz -d C:\MassiveImageArchive --storereport --tessdirectory E:/SmiServices/data/tessdata/
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
