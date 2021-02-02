#!/usr/bin/env bash

set -euo pipefail

dotnet restore

dotnet build \
    -p:Platform=x64 \
    --configuration Release \
    --verbosity quiet
