#!/usr/bin/env python3

import os
import sys

sys.path.append(os.path.join(os.path.dirname(__file__), '..'))
import common as C

import dotnetCommon as DC
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

    # TODO(rkm 2022-02-26) Add --no-build here
    cfg_args = ("-c", args.configuration)
    test_cmd = ("--test", args.test[0]) if args.test else ()
    DT.main((
        *cfg_args,
        "--no-coverage" if args.no_coverage else "",
        *test_cmd
    ))
    DP.main((*cfg_args, args.tag))

    return 0


if __name__ == "__main__":
    raise SystemExit(main())
