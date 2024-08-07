#!/usr/bin/env python3

import argparse
import glob
import json
import os
import platform
import shutil
import subprocess
import sys
from pathlib import Path
from typing import Optional
from typing import Sequence

sys.path.append(os.path.join(os.path.dirname(__file__), '..'))
import common as C

import build as DB
import dotnetCommon as DC
import downloadTessdata


def main(argv: Optional[Sequence[str]] = None) -> int:

    parser = argparse.ArgumentParser()
    DC.add_args(parser)
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
        help="Run unit tests only"
    )
    args, _ = parser.parse_known_args(argv)

    rc = downloadTessdata.main([])
    if rc:
        return rc

    if not args.no_build:
        rc = DB.main(("--configuration", args.configuration))
        if rc:
            return rc

    with open(C.PROJ_ROOT / "global.json") as f:
        global_json = json.load(f)
    sdk_version_major = global_json["sdk"]["version"].split(".")[0]

    netX_glob = {
        Path(x) for x in
        glob.glob(f"{C.PROJ_ROOT}/**/net{sdk_version_major}", recursive=True)
    }
    for build_dir in netX_glob:
        try:
            os.symlink(
                Path(f"{C.PROJ_ROOT}/data/microserviceConfigs/default.yaml").resolve(),
                build_dir / "default.yaml",
            )
        except FileExistsError:
            pass

    cov_dir = C.PROJ_ROOT / "coverage"
    if not args.no_coverage:
        if cov_dir.is_dir():
            shutil.rmtree(cov_dir)

    cmd = ("dotnet", "tool", "restore")
    C.run(cmd)

    f = ("--filter", args.test[0]) if args.test else ()
    unit_only = ("--filter", "FullyQualifiedName\!~SmiServices.IntegrationTests") if args.unit_only else ()
    cmd = (
        "dotnet", "dotnet-coverage", "collect",
        "--settings", "coverage.settings",
        "--",
        "dotnet", "test",
        "--configuration", args.configuration,
        "--no-build" if args.no_build else "",
        *unit_only,
        *f,
    )
    C.run(cmd)

    return 0


if __name__ == "__main__":
    raise SystemExit(main())
