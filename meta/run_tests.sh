#!/usr/bin/env bash

# NOTE(rkm 2020-12-04) This script will exit immediately if and command fails. This is
# different to the old Travis builds which would still run each line and only report
# failures at the end

set -euxo pipefail

# Build and run the dotnet tests

dotnet build -c Release --verbosity quiet

for i in `find . -type d -name netcoreapp3.1`; do
    if [ ! -e $i/default.yaml ]; then
        ln data/microserviceConfigs/default.yaml $i/ ;
    fi
done

find ./tests -iname "*.csproj" -print0 \
    | xargs -0 -L 1 \
    dotnet test \
    -p:Platform=x64 \
    --configuration Release \
    --verbosity normal \
    --no-build \
    --settings data/nunit.runsettings

# Build and run the Java tests

# TODO(rkm 2020-12-04) Is this still needed?
if [ -f lib/java/Util/source/java/org/rsna/util/ChunkedInputStream.java ]; then
    echo "Running iconv on ChunkedInputStream.java"
    iconv -f iso-8859-1 -t utf-8 lib/java/Util/source/java/org/rsna/util/ChunkedInputStream.java > tmpiconv
    mv tmpiconv lib/java/Util/source/java/org/rsna/util/ChunkedInputStream.java
fi

mvn -ntp -q -f src/common/com.smi.microservices.parent/pom.xml test
mvn -ntp -q -f src/microservices/uk.ac.dundee.hic.nerd/ test

# Publish artefacts if this is a tagged commit
if [ ! -z "$TRAVIS_TAG" ]; then
    mkdir -p ./dist
    for platform in linux win; do
        dotnet publish -p:PublishTrimmed=false -c Release -r $platform-x64 -o $(pwd)/dist/$platform-x64 -nologo -v q
    done
    ( cd dist && zip -9r smi-services-win-x64-$TRAVIS_TAG.zip ./win-x64 && tar cf - ./linux-x64 | pigz -3 > smi-services-linux-x64-$TRAVIS_TAG.tar.gz && cd - )
    mvn -ntp -q -f src/common/com.smi.microservices.parent/pom.xml install assembly:single@create-deployable -DskipTests
    mvn -ntp -q -f src/microservices/uk.ac.dundee.hic.nerd/ package -DskipTests
    for filename in $(find ./src -name "*deploy-distribution.zip"); do
        base=$(ls $filename | awk -F'-' '{print $1}' | awk -F'/' '{print $6}')
        mv $filename dist/$base-${TRAVIS_TAG}.zip
    done
    mv src/microservices/uk.ac.dundee.hic.nerd/target/nerd-*.jar dist/nerd-${TRAVIS_TAG}.jar
    ls -lh dist/
fi
