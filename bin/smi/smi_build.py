#!/usr/bin/env python3
import argparse
import os
import sys
from collections.abc import Sequence

import dotnet_common as dc

sys.path.append(os.path.join(os.path.dirname(__file__), ".."))
import common as c  # noqa: E402


def main(argv: Sequence[str] | None = None) -> int:

    parser = argparse.ArgumentParser()
    dc.add_args(parser)
    parser.add_argument(
        "--clean",
        action="store_true",
        help="Cleanup any existing files",
    )
    args = parser.parse_args(argv)

    cmd: tuple[str, ...]

    if args.clean:
        cmd = (
            "dotnet",
            "clean",
            "-p:UseCurrentRuntimeIdentifier=True",
            "--verbosity",
            "quiet",
            "--nologo",
        )
        c.run(cmd)

    # NOTE This isn't necessarily needed, but the lockfile processing isn't
    # otherwise transparent in the build output
    if c.is_ci():
        cmd = (
            "dotnet",
            "restore",
            "-warnaserror",
            "--use-current-runtime",
            "--nologo",
            "--locked-mode",
            "--force",
        )
        c.run(cmd)

    cmd = (
        "dotnet",
        "build",
        "-warnaserror",
        "--use-current-runtime",
        "--configuration",
        args.configuration,
        "--no-restore" if c.is_ci() else "",
        "--verbosity",
        "quiet",
        "--nologo",
    )
    c.run(cmd)

    return 0


if __name__ == "__main__":
    raise SystemExit(main())
