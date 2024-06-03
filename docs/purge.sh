#!/bin/bash

set -e

sudo mysql --user=root <<EOU
DROP USER IF EXISTS 'smi'@'localhost';
DROP DATABASE IF EXISTS smi;
DROP DATABASE IF EXISTS smi_isolation;
DROP DATABASE IF EXISTS DLE_STAGING;
EOU

rm -rf /imaging/conf/rdmp
mkdir -p /imaging/conf/rdmp
