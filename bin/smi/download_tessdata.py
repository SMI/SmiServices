#!/usr/bin/env python3
import argparse
import os
import sys
import urllib.request
from collections.abc import Sequence

sys.path.append(os.path.join(os.path.dirname(__file__), ".."))
import common  # noqa: E402

_PATH = f"{common.PROJ_ROOT}/data/tessdata"
_FILE = "eng.traineddata"
_VERSION = "4.1.0"
_MD5SUM = "57e0df3d84fed9fbf8c7a8e589f8f012"


def main(argv: Sequence[str] | None = None) -> int:

    parser = argparse.ArgumentParser()
    parser.add_argument(
        "--clean",
        action="store_true",
        help="Delete the file and re-download if it already exists",
    )
    args = parser.parse_args(argv)

    output_file = f"{_PATH}/{_FILE}"
    if os.path.isfile(output_file):
        if args.clean:
            os.unlink(output_file)
        else:
            print(f"{output_file} exists")
            common.verify_md5(output_file, _MD5SUM)
            return 0

    url = "https://github.com/tesseract-ocr/tessdata/raw/" f"{_VERSION}/{_FILE}"
    print(f"Downloading {url}")
    urllib.request.urlretrieve(url, output_file)

    common.verify_md5(output_file, _MD5SUM)

    return 0


if __name__ == "__main__":
    raise SystemExit(main())
