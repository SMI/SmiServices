#!/usr/bin/env python3

import argparse
import os
import sys

sys.path.append(os.path.join(os.path.dirname(__file__), '..'))
import common as C

import dotnetCommon as DC
import build as DB
import package as DP
import test as DT


def main() -> int:

    parser = argparse.ArgumentParser()
    C.add_clean_arg(parser)
    C.add_tag_arg(parser)
    DC.add_args(parser, "release")
    parser.add_argument(
        "--skip-tests",
        action="store_true",
    )
    parser.add_argument(
        "--no-coverage",
        action="store_true",
    )
    args, _ = parser.parse_known_args()

    cfg_args = ("-c", args.configuration)

    # Build
    build_args = [*cfg_args,]
    if args.clean:
        build_args.append("--clean")
    rc = DB.main(build_args)
    if rc:
        return rc

    # Test
    if not args.skip_tests:
        rc = DT.main((
            *cfg_args,
            "--no-coverage" if args.no_coverage else "",
            "--no-build",
        ))
        if rc:
            return rc

    # Package
    rc = DP.main((
        *cfg_args,
        "--no-build",
        args.tag,
    ))

    return rc


if __name__ == "__main__":
    raise SystemExit(main())
