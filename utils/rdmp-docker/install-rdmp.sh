#!/bin/bash

set -ev

# NOTE(rkm 2020-02-06) Potential race-condition
/opt/mssql/bin/sqlservr &> /dev/null &
wget -q https://github.com/HicServices/RDMP/releases/download/v4.0.2/rdmp-cli-linux-x64.zip
unzip -d rdmp-cli rdmp-cli-linux-x64.zip -x "Curation*" "zh-*"
chmod +x rdmp-cli/rdmp
rdmp-cli/rdmp install localhost TEST_ -u SA -p $SA_PASSWORD
cat <<EOT > ./rdmp-cli/Databases.yaml
CatalogueConnectionString: Server=localhost;user=SA;password=$SA_PASSWORD;Database=TEST_Catalogue;
DataExportConnectionString: Server=localhost;user=SA;password=$SA_PASSWORD;Database=TEST_DataExport;
EOT
