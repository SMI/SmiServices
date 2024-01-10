#!/bin/bash

set -e

mkdir -p /imaging/{bin/rdmp,bin/smi,conf/rdmp,data} /imaging/db/{mongo,sql} 
curl -sL https://github.com/HicServices/RDMP/releases/download/v8.1.2/rdmp-8.1.2-cli-linux-x64.tar.xz | tar x --strip-components=1 -JC /imaging/bin/rdmp -f - 
curl -sL https://github.com/SMI/SmiServices/releases/download/v5.4.0/smi-services-v5.4.0-linux-x64.tgz | tar x --strip-components=1 -zC /imaging/bin/smi -f - 
mysql --user=root --password=mysqlrootpassword <<EOU
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
/imaging/bin/rdmp/rdmp createnewexternaldatabaseserver LiveLoggingServer_ID "DatabaseType:MySQL:Server=127.0.0.1;Uid=smi;Pwd=SmiSqlPassword;Database=smi" --dir /imaging/conf/rdmp 
/imaging/bin/rdmp/rdmp cmd CreateNewImagingDatasetSuite "DatabaseType:MySQL:Name:smi:Server=127.0.0.1;Uid=smi;Pwd=SmiSqlPassword;Database=smi_images" ./data DicomFileCollectionSource CT_ CT.it false true --dir /imaging/conf/rdmp 
mysql --user=root --password=mysqlrootpassword < schema.sql
for modality in CR CT DX IO MG MR NM OTHER PT PX RF SR US XA 
do 
/imaging/bin/rdmp/rdmp -- -f /dev/stdin --dir /imaging/conf/rdmp <<EOC
Commands: 
        - newobject joininfo columninfo:*${modality}_*ImageTable\`*Series*UID* columninfo:*${modality}_*SeriesTable\`*Series*UID* right null 
        - newobject joininfo columninfo:*${modality}_*SeriesTable\`*Study*UID* columninfo:*${modality}_*StudyTable\`*Study*UID* right null 
        - set tableinfo:*${modality}_*StudyTable\`* IsPrimaryExtractionTable true 
        - createnewclassbasedprocesstask lmd AdjustRaw PrimaryKeyCollisionIsolationMutilation 
        - rename processtask ${modality}_Isolate 
        - setargument processtask TablesToIsolate tableinfo:*${modality}_*Table\`* 
        - setargument processtask IsolationDatabase eds:*iso* 
EOC
done
