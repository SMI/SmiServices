#!/usr/bin/env python3
import argparse
import os
import sys
import urllib.request
from collections.abc import Sequence

sys.path.append(os.path.join(os.path.dirname(__file__), ".."))
import common  # noqa: E402

_CTP_JAR_DIR = f"{common.PROJ_ROOT}/data/ctp"


def main(argv: Sequence[str] | None = None) -> int:

    parser = argparse.ArgumentParser()
    parser.add_argument(
        "version",
    )
    args = parser.parse_args(argv)

    url = (
        "https://github.com/SMI/ctp-anon-cli/releases/download/"
        f"v{args.version}/ctp-anon-cli-{args.version}.jar"
    )
    file = os.path.join(_CTP_JAR_DIR, f"ctp-anon-cli-{args.version}.jar")

    print(f"Downloading {url} to {file}")
    urllib.request.urlretrieve(url, file)

    return 0


if __name__ == "__main__":
    raise SystemExit(main())
