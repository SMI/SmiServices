#!/usr/bin/env python3

import argparse
import glob
import os
import shutil
import sys
from pathlib import Path
from typing import Optional
from typing import Sequence
sys.path.append(os.path.join(os.path.dirname(__file__), '..'))
import common as C

import build as DB
import dotnetCommon as DC
import downloadTessdata


_COV_DIR = C.PROJ_ROOT / "coverage"


def _run_csproj_tests(
    csproj: Path,
    configuration: str,
    no_build: bool,
    coverage: bool,
    *args: str
) -> None:

    cov_json = _COV_DIR / "coverage.json"

    cov_params = ()
    if coverage:
        cov_params = (
            "/p:CollectCoverage=true",
            f'/p:CoverletOutput="{_COV_DIR.resolve()}/"',
            f'/p:MergeWith="{cov_json.resolve()}"',
            '/p:Exclude="[*.Tests]*"',
        )

    cmd = (
        "dotnet",
        "test",
        "-p:Platform=x64",
        "--configuration", configuration,
        "--verbosity", "quiet",
        "--settings", (C.PROJ_ROOT / "data/nunit.runsettings").resolve(),
        "--no-build" if no_build else "",
        csproj,
        *cov_params,
        *args,
    )
    C.run(cmd)


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
    parser.add_argument(
        "--test",
        nargs=1,
        help="Run a specific test class",
    )
    args, _ = parser.parse_known_args(argv)

    downloadTessdata.main([])

    if not args.no_build:
        DB.main(("--configuration", args.configuration))

    # TODO(rkm 2022-02-25) Is there another way around this?
    net6_glob = {
        Path(x) for x in
        glob.glob(f"{C.PROJ_ROOT}/**/net6", recursive=True)
    }
    for build_dir in net6_glob:
        try:
            os.symlink(
                Path(f"{C.PROJ_ROOT}/data/microserviceConfigs/default.yaml").resolve(),
                build_dir / "default.yaml",
            )
        except FileExistsError:
            pass

    if not args.no_coverage:
        if _COV_DIR.is_dir():
            shutil.rmtree(_COV_DIR)
        _COV_DIR.mkdir()

    test_csprojs = [
        Path(x).resolve() for x in
        glob.glob(f"{C.PROJ_ROOT}/tests/**/*.csproj", recursive=True)
    ]

    test_cmd = ("--filter", args.test[0]) if args.test else ()

    for csproj in test_csprojs[:-1]:
        _run_csproj_tests(
            csproj,
            args.configuration,
            args.no_build,
            not args.no_coverage,
            *test_cmd
        )

    # NOTE(rkm 2021-06-01) Run last test with additional option to generate merged opencover file
    _run_csproj_tests(
        test_csprojs[-1],
        args.configuration,
        args.no_build,
        not args.no_coverage,
        '/p:CoverletOutputFormat="opencover"' if not args.no_coverage else "",
        *test_cmd,
    )

    return 0


if __name__ == "__main__":
    raise SystemExit(main())
