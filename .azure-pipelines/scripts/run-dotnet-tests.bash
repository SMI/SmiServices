#!/usr/bin/env bash

# NOTE(rkm 2020-12-04) This script will exit immediately if any command fails. This is
# different to the old Travis builds which would still run each line and only report
# failures at the end

set -uo pipefail

if [ ! -f ./data/tessdata/eng.traineddata ]
then
    echo "Error: tesseract test data missing (data/tessdata/eng.traineddata)"
    exit 1
fi

set -e

.azure-pipelines/scripts/dotnet-build.bash

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
