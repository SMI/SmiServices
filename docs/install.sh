#!/bin/bash
mkdir -p /imaging/{bin/rdmp,bin/smi,conf/rdmp,data} /imaging/db/{mongo,sql} 
curl -sL https://github.com/HicServices/RDMP/releases/download/v8.1.1/rdmp-8.1.1-cli-linux-x64.tar.xz | tar x --strip-components=1 -JC /imaging/bin/rdmp -f - 
curl -sL https://github.com/SMI/SmiServices/releases/download/v5.4.0/smi-services-v5.4.0-linux-x64.tgz | tar x --strip-components=1 -zC /imaging/bin/smi -f - 
mysql --user=root --password=mysqlrootpassword < schema.sql
wget https://raw.githubusercontent.com/HicServices/DicomTypeTranslation/main/Templates/CT.it 
/imaging/bin/rdmp/rdmp createnew externaldatabaseserver LiveLoggingServer_ID "DatabaseType:MySQL:Server=127.0.0.1;Uid=smi;Pwd=SmiSqlPassword;Database=smi" --dir /imaging/conf/rdmp 
/imaging/bin/rdmp/rdmp cmd CreateNewImagingDatasetSuite "DatabaseType:MySQL:Name:smi:Server=127.0.0.1;Uid=smi;Pwd=SmiSqlPassword;Database=smi_images" ./data DicomFileCollectionSource CT_ CT.it false true --dir /imaging/conf/rdmp 
for modality in CR CT DX IO MG MR NM OTHER PT PX RF SR US XA 
do 
sed –e "s/MR_/${modality}_/g" <<'EOC' | /imaging/bin/rdmp/rdmp -f /dev/stdin --dir /imaging/conf/rdmp
Commands: 
        - newobject joininfo columninfo:*MR_*ImageTable`*Series*UID* columninfo:*MR_*SeriesTable`*Series*UID* inner null 
        - newobject joininfo columninfo:*MR_*SeriesTable`*Study*UID* columninfo:*MR_*StudyTable`*Study*UID* inner null 
        - set tableinfo:*MR_*StudyTable`* IsPrimaryExtractionTable true 
        - createnewclassbasedprocesstask lmd AdjustRaw PrimaryKeyCollisionIsolationMutilation 
        - rename processtask MR_Isolate 
        - setargument processtask TablesToIsolate tableinfo:*MR_*Table`* 
        - setargument processtask IsolationDatabase eds:*iso* 
EOC
done
