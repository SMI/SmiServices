#!/usr/bin/env python3
import argparse
import os
import sys

import dotnet_common
import smi_build
import smi_package
import smi_test

sys.path.append(os.path.join(os.path.dirname(__file__), ".."))
import common  # noqa: E402


def main() -> int:

    parser = argparse.ArgumentParser()
    common.add_clean_arg(parser)
    common.add_tag_arg(parser)
    dotnet_common.add_args(parser, "release")
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
    build_args = [*cfg_args]
    if args.clean:
        build_args.append("--clean")
    rc = smi_build.main(build_args)
    if rc:
        return rc

    # Test
    if not args.skip_tests:
        rc = smi_test.main(
            (
                *cfg_args,
                "--no-coverage" if args.no_coverage else "",
                "--no-build",
            ),
        )
        if rc:
            return rc

    # Package
    rc = smi_package.main(
        (
            *cfg_args,
            "--no-build",
            args.tag,
        ),
    )

    return rc


if __name__ == "__main__":
    raise SystemExit(main())
