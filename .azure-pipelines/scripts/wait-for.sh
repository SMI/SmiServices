#!/bin/bash

set -uxo pipefail

if [ "$#" -ne "1" ]
then
    echo "Error: Usage $0 <quoted command>"
    exit 1
fi

set -e

cmd="$@"
timeout 10s bash -c "until $cmd ; do echo -n . && sleep 1 ; done"
