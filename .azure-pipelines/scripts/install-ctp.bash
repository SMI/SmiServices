#!/usr/bin/env bash

set -euxo pipefail

pushd ./lib/java
./installDat.sh
popd
