#!/usr/bin/env bash

set -uo pipefail

if [ $# -ne 1 ]; then
    echo "Usage: $0 docker-compose.yml"
    exit 1
fi

set -ex

compose_file="$(basename "$1")"

docker run \
    --rm \
    -v`pwd`/.azure-pipelines/docker-compose:/run:z \
    --user $(id -u):$(id -g) \
    safewaters/docker-lock \
        lock generate \
        --composefiles="$compose_file" \
        --lockfile-name "${compose_file}.lock"

