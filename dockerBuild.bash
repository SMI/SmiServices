#!/usr/bin/env bash

set -euxo pipefail

sdk_version=$(jq -r .sdk.version global.json)

docker build \
    --build-arg SDK_VERSION=$sdk_version \
    --build-arg RUNTIME_VERSION=$(echo $sdk_version | cut -d'.' -f-2) \
    .
