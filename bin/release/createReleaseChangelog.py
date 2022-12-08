#!/usr/bin/env python3

import argparse
import re
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
            line = f.readline().strip("\n")
            if not reading:
                if tag in line:
                    reading = True
                continue
            if reading and line.startswith("## ["):
                break
            release_lines.append(line)

    content = "\n".join(release_lines)
    # NOTE(rkm 2021-06-03) GH's Releases display doesn't properly wrap markdown
    content = re.sub(r"\n(\s+(?=\w))", " ", content)

    output_file = "release_changelog.md"
    with open(output_file, "w") as f:
        f.write(content)

    print(f"Wrote {output_file}")


if __name__ == "__main__":
    exit(main())
