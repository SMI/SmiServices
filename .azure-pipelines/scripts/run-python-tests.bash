#!/usr/bin/env bash

set -euo pipefail

./venv/bin/python -m pytest src/common/Smi_Common_Python/SmiServices/*.py
