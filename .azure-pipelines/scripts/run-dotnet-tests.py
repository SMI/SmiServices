#!/usr/bin/env python3

import glob
import os
import shutil
import subprocess
import sys
from pathlib import Path
from typing import Sequence
from typing import Union


_STR_LIKE = Union[str, Path]
_COV_DIR = Path("coverage")


def _run(cmd: Sequence[_STR_LIKE]) -> None:

    subprocess.check_call(("echo", "$ ", *cmd))
    subprocess.check_call(cmd)


def _run_tests(test_csproj: Path, *args: str) -> None:

    cov_json = _COV_DIR / "coverage.json"

    cmd: Sequence[_STR_LIKE] = (
        "dotnet",
        "test",
        "/p:Platform=x64",
        "--configuration=Release",
        "--verbosity=Normal",
        "--no-build",
        "--settings=data/nunit.runsettings",
        test_csproj,
        "/p:CollectCoverage=true",
        f'/p:CoverletOutput="{_COV_DIR.resolve()}/"',
        f'/p:MergeWith="{cov_json.resolve()}"',
        *args,
    )
    _run(cmd)


def main() -> int:

    tessdata = Path("./data/tessdata/eng.traineddata")
    if not tessdata.is_file():
        print(
            "Error: tesseract test data missing (data/tessdata/eng.traineddata)",
            file=sys.stderr,
        )
        return 1

    _run((".azure-pipelines/scripts/dotnet-build.bash",))

    # TODO(rkm 2021-05-31) Check if this is still needed
    net5_glob = {Path(x) for x in glob.glob("**/net5", recursive=True)}
    for build_dir in net5_glob:
        try:
            os.symlink(
                Path("data/microserviceConfigs/default.yaml").resolve(),
                build_dir / "default.yaml",
            )
        except FileExistsError:
            pass

    if _COV_DIR.is_dir():
        shutil.rmtree(_COV_DIR)
    _COV_DIR.mkdir()

    test_csprojs = [Path(x) for x in glob.glob("tests/**/*.csproj", recursive=True)]
    for csproj in test_csprojs[:-1]:
        _run_tests(csproj)
    _run_tests(test_csprojs[-1], '/p:CoverletOutputFormat="opencover"')

    return 0


if __name__ == "__main__":
    exit(main())
