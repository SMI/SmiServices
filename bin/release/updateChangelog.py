#!/usr/bin/env python3
"""
Automatically updates the CHANGELOG with the fragments from each news file.
"""
import argparse
import collections
import datetime
import fileinput
import json
import subprocess
import sys
import urllib.request
from pathlib import Path
from typing import Dict
from typing import Optional
from typing import Sequence
from typing import Union


_NEWS_DIR = Path("./news/")
_MARKER = "<!--next-->"
_UNRELEASED_LINK = "[unreleased]:"

_ORG = "SMI"
_REPO = "SmiServices"

_STR_LIKE = Union[str, Path]


def _run(cmd: Sequence[_STR_LIKE]):
    subprocess.check_call(("echo", *cmd))
    subprocess.check_call(cmd)


def _get_pr_author(pr_ref: int) -> str:

    url = (
        f"https://api.github.com/repos/"
        f"{_ORG}/{_REPO}/"
        f"pulls/{pr_ref}"
    )
    try:
        resp = urllib.request.urlopen(url)
    except urllib.error.HTTPError as e:
        raise Exception(f"Could not open {url}") from e
    data = json.loads(resp.read().decode())
    return data["user"]["login"]


def _print_fragments(version: str, fragments: Dict[str, Dict[int, str]]) -> None:

    today = datetime.datetime.today().strftime("%Y-%m-%d")
    print(f"## [{version[1:]}] {today}")

    def _print_type_fragment(frag_type: str, fragments: Dict[str, Dict[int, str]]):
        print(f"\n### {frag_type.capitalize()}\n")
        for pr_ref in sorted(fragments[frag_type]):
            first, *rest = fragments[frag_type][pr_ref].splitlines()
            list_items = ""
            for li in rest:
                list_items += f"\n    {li}"
            author = _get_pr_author(pr_ref)
            line = (
                "-   "
                f"[#{pr_ref}](https://github.com/{_ORG}/{_REPO}/pull/{pr_ref})"
                f" by {author}. "
                f"{first}"
                f"{list_items}"
            )
            print(line)

    # New features first
    if "feature" in fragments:
        _print_type_fragment("feature", fragments)

    # Then the rest
    for frag_type in sorted(fragments):
        if frag_type == "feature":
            continue
        _print_type_fragment(frag_type, fragments)


def _print_links(last_tag: str, next_tag: str) -> None:

    unreleased_str = (
        "[unreleased]: "
        f"https://github.com/{_ORG}/{_REPO}/compare/{next_tag}"
        "...main"
    )
    print(unreleased_str)

    diff_link_str = (
        f"[{next_tag[1:]}]: "
        f"https://github.com/{_ORG}/{_REPO}/compare/"
        f"{last_tag}...{next_tag}"
    )
    print(diff_link_str)


def main(argv: Optional[Sequence[str]] = None) -> int:

    parser = argparse.ArgumentParser()
    parser.add_argument(
        "last_tag",
        help="The tag for the last release",
    )
    parser.add_argument(
        "next_tag",
        help="The tag for the next release",
    )
    args = parser.parse_args(argv)

    # Gather the news files for the next release
    fragments = collections.defaultdict(dict)
    fragment_files = list(_NEWS_DIR.glob("*-*.md"))
    for fragment_file in fragment_files:
        with open(fragment_file) as f:
            contents = f.read()
        pr_ref, _, frag_type = fragment_file.stem.partition("-")
        fragments[frag_type][pr_ref] = contents

    # Write-out the new CHANGELOG
    with fileinput.FileInput("CHANGELOG.md", inplace=True) as f:
        for line in f:
            if _MARKER in line:
                print(line)
                _print_fragments(args.next_tag, fragments)
                continue
            elif line.startswith(_UNRELEASED_LINK):
                _print_links(args.last_tag, args.next_tag)
                continue
            print(line, end="")

    # Now delete all the news files
    for news_file in fragment_files:
        cmd = (
            "git", "rm",
            news_file
        )
        _run(cmd)

    return 0


if __name__ == "__main__":
    exit(main())
