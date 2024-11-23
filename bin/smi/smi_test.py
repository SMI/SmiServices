#!/usr/bin/env python3
import argparse
import glob
import json
import os
import shutil
import sys
from collections.abc import Sequence

import dotnet_common
import download_tessdata
import smi_build

sys.path.append(os.path.join(os.path.dirname(__file__), ".."))
import common  # noqa: E402


def main(argv: Sequence[str] | None = None) -> int:

    parser = argparse.ArgumentParser()
    dotnet_common.add_args(parser)
    parser.add_argument(
        "--no-build",
        action="store_true",
    )
    parser.add_argument(
        "--no-coverage",
        action="store_true",
    )
    group = parser.add_mutually_exclusive_group()
    group.add_argument(
        "--test",
        nargs=1,
        help="Run a specific test class",
    )
    group.add_argument(
        "--unit-only",
        action="store_true",
        help="Run unit tests only",
    )
    args, _ = parser.parse_known_args(argv)

    rc = download_tessdata.main([])
    if rc:
        return rc

    if not args.no_build:
        rc = smi_build.main(("--configuration", args.configuration))
        if rc:
            return rc

    with open(f"{common.PROJ_ROOT}/global.json") as f:
        global_json = json.load(f)
    sdk_version_major = global_json["sdk"]["version"].split(".")[0]

    net_glob = {
        x
        for x in glob.glob(
            f"{common.PROJ_ROOT}/**/net{sdk_version_major}",
            recursive=True,
        )
    }
    for build_dir in net_glob:
        try:
            os.symlink(
                f"{common.PROJ_ROOT}/data/microserviceConfigs/default.yaml",
                f"{build_dir}/default.yaml",
            )
        except FileExistsError:
            pass

    cov_dir = f"{common.PROJ_ROOT}/coverage"
    if not args.no_coverage:
        if os.path.isdir(cov_dir):
            shutil.rmtree(cov_dir)

    cmd = ("dotnet", "tool", "restore")
    common.run(cmd)

    filter: tuple[str, ...] = ("--filter", args.test[0]) if args.test else ()
    unit_only = (
        ("--filter", "FullyQualifiedName!~SmiServices.IntegrationTests")
        if args.unit_only
        else ()
    )
    cmd = (
        "dotnet",
        "dotnet-coverage",
        "collect",
        "--settings",
        "coverage.settings",
        "--",
        "dotnet",
        "test",
        "-p:UseCurrentRuntimeIdentifier=True",
        "-warnaserror",
        "--configuration",
        args.configuration,
        "--no-build" if args.no_build else "",
        *unit_only,
        *filter,
    )
    common.run(cmd)

    return 0


if __name__ == "__main__":
    raise SystemExit(main())
