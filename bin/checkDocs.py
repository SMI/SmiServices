#!/usr/bin/env python3
import collections
import os
import re

import common as c

_VERB_CS_PATH = f"{c.PROJ_ROOT}/src/SmiServices/ServiceVerbs.cs"
_VERB_RE = re.compile(r'\[Verb\(("\S*?")')


def check_doc_content(doc_path: str) -> int:

    has_flow = has_yaml = has_cli = False

    with open(doc_path) as f:
        for line in f.read().splitlines():
            if line == "## Message Flow":
                has_flow = True
            elif line == "## YAML Configuration":
                has_yaml = True
            elif line == "## CLI Options":
                has_cli = True

    rc = 0
    if not has_flow:
        print(f"{doc_path}: Missing 'Message Flow' section")
        rc = 1

    if not has_yaml:
        print(f"{doc_path}: Missing 'YAML Configuration' section")
        rc = 1

    if not has_cli:
        print(f"{doc_path}: Missing 'CLI Options' section")
        rc = 1

    return rc


def main() -> int:

    tools = collections.defaultdict(list)
    tool_type = "application"
    with open(_VERB_CS_PATH) as f:
        for line in f.read().splitlines():

            if "region Microservices" in line:
                tool_type = "service"
                continue

            match = _VERB_RE.search(line)
            if not match:
                continue

            tools[tool_type].append(match.group(1).strip('"'))

    rc = 0

    for tool_type in ("application", "service"):
        for tool in tools[tool_type]:
            doc_path = f"{c.PROJ_ROOT}/docs/{tool_type}s/{tool}.md"
            if not os.path.isfile(doc_path):
                print(f"Missing {doc_path}")
                rc |= 1
                continue

            rc |= check_doc_content(doc_path)

    return rc


if __name__ == "__main__":
    raise SystemExit(main())
