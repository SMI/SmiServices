#!/usr/bin/env python3

import collections
import glob
import os
import re
import sys

sys.path.append(os.path.join(os.path.dirname(__file__), '..'))
import common as C


_VERB_CS_PATH = f"{C.PROJ_ROOT}/src/applications/Applications.SmiRunner/ServiceVerbs.cs"
_VERB_RE = re.compile(r'\[Verb\(("\S*?")')


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
            doc_path = f"{C.PROJ_ROOT}/docs/{tool_type}s/{tool}.md"
            if not os.path.isfile(doc_path):
                print(f"Missing {doc_path}")
                rc = 1

    return rc


if __name__ == "__main__":
    raise SystemExit(main())
