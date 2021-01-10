#!/usr/bin/env bash

set -euxo pipefail

dotnet build \
    -c Release \
    --verbosity quiet
