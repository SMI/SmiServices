# Feature/Dicom-Anonymiser

## Pre-Requisites

  VirtualEnvPath: "/Users/daniyalarshad/EPCC/github/NationalSafeHaven/venv"
  DicomPixelAnonPath: "/Users/daniyalarshad/EPCC/github/NationalSafeHaven/dicompixelanon/src/applications/"
  SmiServicesPath: "/Users/daniyalarshad/EPCC/github/SmiServices" # replace with /nfs/smi/home/smi for production
  CtpAnonCliJar: "/Users/daniyalarshad/EPCC/github/ctp-anon-cli/ctp-anon-cli-0.5.0.jar" # replace with /nfs/smi/home/smi/bin/ctp-anon-cli-0.5.0.jar for production
  CtpAllowlistScript: "/Users/daniyalarshad/EPCC/github/SmiServices/data/ctp/ctp-whitelist.script" # replace with /nfs/smi/home/smi/configs/ctp-whitelist.script for production
  SRAnonymiserToolPath: "/Users/daniyalarshad/EPCC/github/StructuredReports/src/applications/SRAnonTool/CTP_SRAnonTool.sh"

**1. Docker (Container)**
   
Initiate Essential Services (RabbitMQ, MSSQL, MongoDB, MariaDB, Redis)

```
git clone https://github.com/SMI/SmiServices.git
cd SmiServices/utils/docker-compose
docker-compose -f linux-dotnet-arm.yml up
```

**2. RDMP (Application)**

Setup RDMP (Github)

```
git clone git@github.com:HicServices/RDMP.git
cd RDMP/Tools/rdmp
dotnet clean
dotnet build
cd bin/Debug/net8.0
./rdmp install localhost RDMP_ -e -D -u sa -p "YourStrongPassw0rd"
```

Update Databases Connection (.yaml)

```
cd RDMP/Tools/rdmp
cat > Databases.yaml << EOF
CatalogueConnectionString: Server=localhost;Database=RDMP_Catalogue;User ID=sa;Password=YourStrongPassw0rd;Trust Server Certificate=true;
DataExportConnectionString: Server=localhost;Database=RDMP_DataExport;User ID=sa;Password=YourStrongPassw0rd;Trust Server Certificate=true;
EOF
```

**3. Extract Images (Application)**

Sample Image (.dcm)

> Add Sample Image Here

Generate UIDs List (.csv)

```
cd SmiServices/src/microservices/Microservices.DicomAnonymiser/Development/AnonymiserData/extractRoot
cat > extractme.csv << _EOF 
SOPInstanceUID
1.2.840.113619.2.1.2411.1031152382.365.1.736169244.dcm
_EOF
```

Check RDMPOptions (smi-dataExtract.yaml)

Check if the _CatalogueConnectionString_ and _DataExportConnectionString_ are properly set.

```
cd SmiServices/src/microservices/Microservices.DicomAnonymiser/Development/AnonymiserConfig/
cat smi-dataExtract.yaml

// CatalogueConnectionString: 'Server=localhost;Database=RDMP_Catalogue;User ID=sa;Password=YourStrongPassw0rd;Trust Server Certificate=true;'
//DataExportConnectionString: 'Server=localhost;Database=RDMP_DataExport;User ID=sa;Password=YourStrongPassw0rd;Trust Server Certificate=true;'
```

Run Extract Images (.smi)

```
cd SmiServices/src/applications/Applications.SmiRunner/bin/net7.0

./smi extract-images -y /Users/daniyalarshad/EPCC/github/SmiServices/src/microservices/Microservices.DicomAnonymiser/Development/AnonymiserConfig/smi-dataExtract.yaml -p "project-001" -c /Users/daniyalarshad/EPCC/github/SmiServices/src/microservices/Microservices.DicomAnonymiser/Development/AnonymiserData/extractRoot/extractme.csv
```

**4. Cohort Extractor (Microservice)**

Check CohortExtractorOptions (smi-dataExtract.yaml)

```
cd SmiServices/src/microservices/Microservices.DicomAnonymiser/Development/AnonymiserConfig/
cat smi-dataExtract.yaml

// RequestFulfillerType: "Microservices.CohortExtractor.Execution.RequestFulfillers.FakeFulfiller"
```

Run Cohort Extractor (.smi)

```
cd SmiServices/src/applications/Applications.SmiRunner/bin/net7.0

./smi cohort-extractor -y /Users/daniyalarshad/EPCC/github/SmiServices/src/microservices/Microservices.DicomAnonymiser/Development/AnonymiserConfig/smi-dataExtract.yaml
```

**5. Dicom Anonymiser (Microservice)**

Run DICOM Anonymiser (.smi)

```
./smi dicom-anonymiser -y /Users/daniyalarshad/EPCC/github/SmiServices/src/microservices/Microservices.DicomAnonymiser/Development/AnonymiserConfig/smi-dataExtract.yaml
```

View Anonymised DICOMS

> Assuming you have the NationalSafeHaven repository

```
export SMI_ROOT="/Users/daniyalarshad/EPCC/github/NationalSafeHaven/SmiServices"
cd github/NationalSafeHaven
source venv/bin/activate
cd dicompixelanon/src/applications
./dcmaudit.py -i <file-path>
```