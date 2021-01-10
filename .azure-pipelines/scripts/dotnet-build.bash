#!/usr/bin/env bash

set -euo pipefail

dotnet build \
    -p:Platform=x64 \
    --configuration Release \
    --verbosity quiet
