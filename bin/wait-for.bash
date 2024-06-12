#!/usr/bin/env bash

# Runs the specified command until it succeeds or the timeout is reached

set -uo pipefail

if [ $# -lt 1 ]; then
    echo "Error: Usage $0 [timeoout] <quoted command>"
    exit 1
fi

set -e

timeout="30s"

set +x
if [ $# -gt 1 ]; then
    while [ $# -gt 1 ]; do
        case $1 in
            -t|--timeout)
                timeout="$2"
                shift 2
                break
                ;;
            -*|--*)
                echo "Unknown option $1"
                exit 1
                ;;
        esac
    done
fi

set -x
cmd="$1"

timeout "$timeout" bash -c "until $cmd ; do echo -n . && sleep 1 ; done"
