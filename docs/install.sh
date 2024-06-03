#!/bin/bash

set -e

mkdir -p /imaging/{bin/rdmp,bin/smi,conf/rdmp,conf/smi,data} /imaging/db/{mongo,sql} 
curl -sL https://github.com/HicServices/RDMP/releases/download/v8.1.4/rdmp-8.1.4-cli-linux-x64.tar.xz | tar x --strip-components=1 -JC /imaging/bin/rdmp -f - 
curl -sL https://github.com/SMI/SmiServices/releases/download/v5.4.0/smi-services-v5.4.0-linux-x64.tgz | tar x --strip-components=1 -zC /imaging/bin/smi -f - 
sudo mysql --user=root <<EOU
CREATE USER 'smi'@'localhost' IDENTIFIED BY 'SmiSqlPassword';

CREATE DATABASE IF NOT EXISTS smi; 
GRANT ALL ON smi.* TO 'smi'@'localhost';

CREATE DATABASE IF NOT EXISTS smi_isolation; 
GRANT ALL ON smi_isolation.* TO 'smi'@'localhost'; 

CREATE DATABASE IF NOT EXISTs DLE_STAGING;
GRANT ALL ON DLE_STAGING.* TO 'smi'@'localhost'; 

set global log_bin_trust_function_creators =ON;
EOU
wget https://raw.githubusercontent.com/HicServices/DicomTypeTranslation/main/Templates/CT.it 
/imaging/bin/rdmp/rdmp -- -f /dev/stdin --dir /imaging/conf/rdmp <<EOS
Commands:
  - createnewexternaldatabaseserver LiveLoggingServer_ID "DatabaseType:MySQL:Server=127.0.0.1;Uid=smi;Pwd=SmiSqlPassword;Database=smi"
  - createnewexternaldatabaseserver None "DatabaseType:MySQL:Server=127.0.0.1;Uid=smi;Pwd=SmiSqlPassword;Database=smi_isolation"
  - CreateNewImagingDatasetSuite "DatabaseType:MySQL:Name:smi:Server=127.0.0.1;Uid=smi;Pwd=SmiSqlPassword;Database=smi" ./data DicomFileCollectionSource CT_ CT.it false true
EOS
sudo mysql --user=root < schema.sql
for modality in CR CT DX IO MG MR NM OTHER PT PX RF SR US XA 
do 
egrep -v '(OTHER|SR)_\*?(Series|Study)Table' <<EOC | /imaging/bin/rdmp/rdmp -- -f /dev/stdin --dir /imaging/conf/rdmp
Commands:
  - ImportTableInfo "table:${modality}_ImageTable:DatabaseType:MySql:Name:smi:Server=127.0.0.1;Uid=smi;Pwd=SmiSqlPassword;Database=smi" true
  - ImportTableInfo "table:${modality}_SeriesTable:DatabaseType:MySql:Name:smi:Server=127.0.0.1;Uid=smi;Pwd=SmiSqlPassword;Database=smi" true
  - ImportTableInfo "table:${modality}_StudyTable:DatabaseType:MySql:Name:smi:Server=127.0.0.1;Uid=smi;Pwd=SmiSqlPassword;Database=smi" true
  - newobject joininfo columninfo:*${modality}_*ImageTable\`*Series*UID* columninfo:*${modality}_*SeriesTable\`*Series*UID* right null 
  - newobject joininfo columninfo:*${modality}_*SeriesTable\`*Study*UID* columninfo:*${modality}_*StudyTable\`*Study*UID* right null 
  - set tableinfo:*${modality}_*StudyTable\`* IsPrimaryExtractionTable true 
EOC
done

/imaging/bin/rdmp/rdmp -- -f /dev/stdin --dir /imaging/conf/rdmp <<EOS
Commands:
  - createnewclassbasedprocesstask lmd:* AdjustRaw PrimaryKeyCollisionIsolationMutilation 
  - rename processtask Isolate 
  - setargument processtask TablesToIsolate tableinfo:*_*Table\`* 
  - setargument processtask IsolationDatabase eds:2
  - set tableinfo:*SR_*ImageTable\`* IsPrimaryExtractionTable true 
  - set tableinfo:*OTHER_*ImageTable\`* IsPrimaryExtractionTable true 
EOS

lmd=$(ls /imaging/conf/rdmp/LoadMetadata/|tr -dc '[:digit:]')
sed -e "s/LoadMetadataId: 1/LoadMetadataId: ${lmd}/" < config.yaml > /imaging/conf/smi/config.yaml
