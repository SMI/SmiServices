#!/usr/bin/env python3
import argparse
import os
import sys

import ctp_build
import ctp_package
import ctp_test
import java_common

sys.path.append(os.path.join(os.path.dirname(__file__), ".."))
import common  # noqa: E402


def main() -> int:

    parser = argparse.ArgumentParser()
    common.add_clean_arg(parser)
    common.add_tag_arg(parser)
    java_common.add_common_args(parser)
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
        rc = ctp_build.main(build_args)
    else:
        if args.skip_integration_tests:
            build_args.append("-PunitTests")
        rc = ctp_test.main(build_args)

    if rc:
        return rc

    # Package
    rc = ctp_package.main((args.tag,))

    return 0


if __name__ == "__main__":
    raise SystemExit(main())
