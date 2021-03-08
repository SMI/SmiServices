#!/usr/bin/env bash
# Requires an environment with pytest installed,
# plus all the requirements of the SmiServices package.

set -euo pipefail

python -m pytest src/common/Smi_Common_Python/SmiServices/*.py
