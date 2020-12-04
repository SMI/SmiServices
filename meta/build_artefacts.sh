#!/usr/bin/env bash

RELEASE_TAG="$1"
if [ -z $RELEASE_TAG ]; then
    echo "Error: Must pass the release tag"
    exit 1
fi

if [ -d ./dist ]; then
    echo "Error: ./dist already exists"
    exit 1
fi

echo "Building artefacts for ${RELEASE_TAG}"

set -euxo pipefail

mkdir -p ./dist

for platform in linux win; do
    dotnet publish \
        -p:PublishTrimmed=false \
        --configuration Release \
        --runtime $platform-x64 \
        --output $(pwd)/dist/smi-services-${RELEASE_TAG}-${platform}-x64 \
        -v quiet \
        --nologo
done

mvn -ntp -q -f ./src/common/com.smi.microservices.parent/pom.xml install assembly:single@create-deployable -DskipTests
mvn -ntp -q -f ./src/microservices/uk.ac.dundee.hic.nerd/ package -DskipTests

for filename in $(find ./src -name "*deploy-distribution.zip"); do
    base=$(ls $filename | awk -F'-' '{print $1}' | awk -F'/' '{print $6}')
    mv $filename ./dist/$base-${RELEASE_TAG}.zip
done

mv ./src/microservices/uk.ac.dundee.hic.nerd/target/nerd-*.jar ./dist/smi-nerd-${RELEASE_TAG}.jar

pushd dist
tar cf - ./smi-services-${RELEASE_TAG}-linux-x64 | pigz > smi-services-${RELEASE_TAG}-linux-x64.tgz
zip -9r smi-services-${RELEASE_TAG}-win-x64.zip ./smi-services-${RELEASE_TAG}-win-x64
rm -r smi-services-${RELEASE_TAG}-linux-x64 smi-services-${RELEASE_TAG}-win-x64
md5sum * > MD5SUMS.txt
cat MD5SUMS.txt
popd
