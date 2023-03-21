#!/usr/bin/env python3

import argparse
import glob
import os
import shutil
import sys
from pathlib import Path

sys.path.append(os.path.join(os.path.dirname(__file__), '..'))
import common as C

import build as JB
import package as JP
import test as JT
import javaCommon as JC
import installLibs as L


def main() -> int:

    parser = argparse.ArgumentParser()
    C.add_clean_arg(parser)
    C.add_tag_arg(parser)
    JC.add_common_args(parser)
    parser.add_argument(
        "--skip-tests",
        action="store_true",
    )
    parser.add_argument(
        "--skip-integration-tests",
        action="store_true",
    )
    args, _ = parser.parse_known_args()

    build_args = []
    if args.clean:
        build_args.append("--clean")
    if args.install_libs:
        build_args.append("--install-libs")

    # Build and test
    if args.skip_tests:
        rc = JB.main(build_args)
    else:
        if args.skip_integration_tests:
            build_args.append("-PunitTests")
        rc = JT.main(build_args)

    if rc:
        return rc

    # Package
    rc = JP.main((args.tag,))

    return 0


if __name__ == "__main__":
    raise SystemExit(main())
