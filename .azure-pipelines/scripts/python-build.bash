#!/usr/bin/env bash

set -euxo pipefail

venv/bin/python src/common/Smi_Common_Python/setup.py bdist_wheel
