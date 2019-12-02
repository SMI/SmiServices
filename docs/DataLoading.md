# Data Loading

## Contents

- [Background](#background)
- [Preparation](#preparation)

## Background

This document describes all the steps required to setup data load microservices and use them to load a collection of Dicom images.

### Preparation

Download [Bad Dicom](https://github.com/HicServices/BadMedicine.Dicom/releases) and use it to generate some test images on disk:

```
BadDicom.exe c:\temp\testdicoms
```

![Test files in file explorer (windows)](./Images/DataLoading/testfiles.png)

