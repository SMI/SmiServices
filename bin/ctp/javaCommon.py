"""Common variables and functions for java"""

import argparse
import os


def add_common_args(
    parser: argparse.ArgumentParser,
) -> None:
    parser.add_argument(
        "--install-libs",
        action="store_true",
        help="Install the CTP libraries if specified"
    )

def mvn_exe() -> str:
    return  "mvn" if os.name == "posix" else "mvn.cmd"
