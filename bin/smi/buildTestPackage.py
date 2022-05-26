#!/usr/bin/env python3

import os
import sys

sys.path.append(os.path.join(os.path.dirname(__file__), '..'))
import common as C

import dotnetCommon as DC
import build as DB
import package as DP
import test as DT


def main() -> int:

    parser = C.get_parser()
    DC.add_args(parser, "release")
    parser.add_argument(
        "--test",
        nargs=1,
        help="Run a specific test class",
    )
    parser.add_argument(
        "--no-coverage",
        action="store_true",
    )
    args, _ = parser.parse_known_args()

    cfg_args = ("-c", args.configuration)

    # Clean and Build
    rc = DB.main((*cfg_args, "--clean"))
    if rc:
        return rc

    # Test
    test_cmd = ("--test", args.test[0]) if args.test else ()
    rc = DT.main((
        *cfg_args,
        "--no-coverage" if args.no_coverage else "",
        "--no-build",
        *test_cmd
    ))
    if rc:
        return rc

    # Package
    rc = DP.main((*cfg_args, args.tag))

    return rc


if __name__ == "__main__":
    raise SystemExit(main())
