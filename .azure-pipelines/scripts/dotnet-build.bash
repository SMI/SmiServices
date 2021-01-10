#!/usr/bin/env bash

set -euxo pipefail

dotnet build \
    -p:Platform=x64 \
    --configuration Release \
    --verbosity quiet
