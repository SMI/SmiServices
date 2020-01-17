#!/bin/bash

# TODO(rkm 2020-01-17) Not sure how this should work for local development - maybe use an envrionment variable?

set -e

NER_VER="2016-10-31"

[ -d "stanford-ner-$NER_VER" ] && exit 0

wget http://nlp.stanford.edu/software/stanford-ner-$NER_VER.zip
unzip stanford-ner-$NER_VER.zip
rm stanford-ner-$NER_VER.zip
