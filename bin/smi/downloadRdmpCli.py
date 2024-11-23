#!/usr/bin/env python3
import argparse
import os
import shutil
import stat
import sys
import tempfile
import urllib.request
import zipfile
from collections.abc import Sequence

sys.path.append(os.path.join(os.path.dirname(__file__), ".."))
import common  # noqa: E402

_RDMP_CLI_DIR = f"{common.PROJ_ROOT}/rdmp-cli"


def main(argv: Sequence[str] | None = None) -> int:

    parser = argparse.ArgumentParser()
    parser.add_argument(
        "version",
    )
    parser.add_argument(
        "--clean",
        action="store_true",
        help="Delete the file and re-download if it already exists",
    )
    args = parser.parse_args(argv)

    if args.clean:
        shutil.rmtree(_RDMP_CLI_DIR)
    elif os.path.isdir(_RDMP_CLI_DIR):
        print(f"Error: {_RDMP_CLI_DIR} exists")
        return 1
    os.mkdir(_RDMP_CLI_DIR)

    platform = "linux" if os.name == "posix" else "win"
    url = (
        "https://github.com/HicServices/RDMP/releases/download/"
        f"v{args.version}/rdmp-{args.version}-cli-{platform}-x64.zip"
    )
    print(f"Downloading {url}")
    with tempfile.TemporaryDirectory() as tmpdir:
        _file = f"{tmpdir}/rdmp-cli-{args.version}-x64.zip"
        urllib.request.urlretrieve(url, _file)

        with zipfile.ZipFile(_file) as zf:
            zf.extractall(path=_RDMP_CLI_DIR)

    rdmp = f"{_RDMP_CLI_DIR}/{'rdmp.exe' if os.name == 'nt' else 'rdmp'}"
    st = os.stat(rdmp)
    os.chmod(rdmp, st.st_mode | stat.S_IXUSR | stat.S_IXGRP)

    # TODO(rkm 2022-02-26) Checksum

    return 0


if __name__ == "__main__":
    raise SystemExit(main())
