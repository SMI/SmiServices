#!/bin/bash

set -e

FILE="eng.traineddata"

[ -f "$FILE" ] && exit 0

wget -q https://github.com/tesseract-ocr/tessdata/raw/master/$FILE
