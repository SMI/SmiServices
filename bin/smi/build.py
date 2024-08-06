#!/usr/bin/env python3

import argparse
import os
import platform
import sys
from typing import Optional
from typing import Sequence

sys.path.append(os.path.join(os.path.dirname(__file__), '..'))
import common as C

import dotnetCommon as DC


def main(argv: Optional[Sequence[str]] = None) -> int:

    parser = argparse.ArgumentParser()
    DC.add_args(parser)
    parser.add_argument(
        "--clean",
        action="store_true",
        help="Cleanup any existing files",
    )
    args = parser.parse_args(argv)

    if args.clean:
        cmd = (
            "dotnet",
            "clean",
            "--verbosity", "quiet",
            "--nologo",
        )
        C.run(cmd)

    cmd = (
        "dotnet",
        "build",
        "--use-current-runtime",
        "--configuration", args.configuration,
        "--verbosity", "quiet",
        "--nologo",
    )
    C.run(cmd)

    return 0


if __name__ == "__main__":
    raise SystemExit(main())
