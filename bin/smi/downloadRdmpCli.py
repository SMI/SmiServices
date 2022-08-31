#!/usr/bin/env python3

import argparse
import os
import shutil
import stat
import sys
import tempfile
import urllib.request
import zipfile
from pathlib import Path
from typing import Sequence
from typing import Optional

sys.path.append(os.path.join(os.path.dirname(__file__), '..'))
import common as C

_RDMP_CLI_DIR = (C.PROJ_ROOT / "rdmp-cli").resolve()


def main(argv: Optional[Sequence[str]] = None) -> int:

    parser = argparse.ArgumentParser()
    parser.add_argument(
        "version",
    )
    parser.add_argument(
        "--clean",
        action="store_true",
        help="Delete the file and re-download if it already exists"
    )
    args = parser.parse_args(argv)

    if args.clean:
        shutil.rmtree(_RDMP_CLI_DIR)
    elif _RDMP_CLI_DIR.is_dir():
        print(f"Error: {_RDMP_CLI_DIR} exists")
        return 1
    _RDMP_CLI_DIR.mkdir()

    platform = "linux" if os.name == "posix" else "win"
    url = (
        "https://github.com/HicServices/RDMP/releases/download/"
        f"{args.version}/rdmp-{args.version}-cli-{platform}-x64.zip"
    )
    print(f"Downloading {url}")
    with tempfile.TemporaryDirectory() as tmpdir:
        _file = Path(tmpdir) / f"rdmp-cli-{args.version}-x64.zip"
        urllib.request.urlretrieve(url, _file)

        with zipfile.ZipFile(_file) as zf:
            zf.extractall(path=_RDMP_CLI_DIR)

    rdmp = _RDMP_CLI_DIR / ("rdmp.exe" if os.name == "nt" else "rdmp")
    st = os.stat(rdmp)
    os.chmod(rdmp, st.st_mode | stat.S_IXUSR | stat.S_IXGRP)

    # TODO(rkm 2022-02-26) Checksum

    return 0


if __name__ == "__main__":
    raise SystemExit(main())
