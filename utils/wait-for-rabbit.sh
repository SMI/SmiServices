#!/bin/bash
# NOTE(rkm 2020-01-30) Naive - Doesn't actually return non-zero if we timeout...
NEXT_WAIT_TIME=0
until \
curl -i -u guest:guest http://localhost:15672/api/healthchecks/node &> /dev/null \
|| [ $NEXT_WAIT_TIME -eq 30 ]; do
    sleep $(( NEXT_WAIT_TIME++ ))
done
