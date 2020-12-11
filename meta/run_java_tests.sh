#!/usr/bin/env bash

# NOTE(rkm 2020-12-04) This script will exit immediately if and command fails. This is
# different to the old Travis builds which would still run each line and only report
# failures at the end

set -euo pipefail

pushd ./lib/java
./installDat.sh
popd

# TODO(rkm 2020-12-04) Is this still needed?
if [ -f lib/java/Util/source/java/org/rsna/util/ChunkedInputStream.java ]; then
    echo "Running iconv on ChunkedInputStream.java"
    iconv -f iso-8859-1 -t utf-8 lib/java/Util/source/java/org/rsna/util/ChunkedInputStream.java > tmpiconv
    mv tmpiconv lib/java/Util/source/java/org/rsna/util/ChunkedInputStream.java
fi

mvn -ntp -q -f src/common/com.smi.microservices.parent/pom.xml test
mvn -ntp -q -f src/microservices/uk.ac.dundee.hic.nerd/ test
