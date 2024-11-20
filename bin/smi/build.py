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
    # "clean" needs an argument due to
    # https://github.com/dotnet/sdk/issues/27488
    parser.add_argument(
        "--clean",
        metavar="RID",
        help="Cleanup any existing files. You must specify the RID",
    )
    args = parser.parse_args(argv)

    if args.clean is not None:
        cmd = (
            "dotnet",
            "clean",
            "--runtime", args.clean,
            "--verbosity", "quiet",
            "--nologo",
        )
        C.run(cmd)

    # NOTE This isn't necessarily needed, but the lockfile processing isn't
    # otherwise transparent in the build output
    if C.is_ci():
        cmd = (
            "dotnet",
            "restore",
            "-warnaserror",
            "--use-current-runtime",
            "--nologo",
            "--locked-mode",
            "--force",
        )
        C.run(cmd)

    cmd = (
        "dotnet",
        "build",
        "-warnaserror",
        "--use-current-runtime",
        "--configuration", args.configuration,
        "--no-restore" if C.is_ci() else "",
        "--verbosity", "quiet",
        "--nologo",
    )
    C.run(cmd)

    return 0


if __name__ == "__main__":
    raise SystemExit(main())
