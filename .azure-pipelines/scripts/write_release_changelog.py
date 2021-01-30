#!/usr/bin/env python3

import argparse
from typing import Optional
from typing import Sequence


def main(argv: Optional[Sequence[str]] = None) -> int:

    parser = argparse.ArgumentParser()
    parser.add_argument("tag", help="The git tag for the release")
    args = parser.parse_args(argv)
    tag = f"[{args.tag[1:]}]"
    print(f"Scanning for '{tag}'")

    release_lines = []
    with open("CHANGELOG.md") as f:
        reading = False
        while True:
            line = f.readline().strip()
            if not reading:
                if tag in line:
                    reading = True
                continue
            if reading and line.startswith("## ["):
                break
            release_lines.append(line)

    with open("release_changelog.md", "w") as f:
        f.write("\n".join(release_lines))


if __name__ == "__main__":
    exit(main())

