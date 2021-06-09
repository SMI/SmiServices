#!/usr/bin/env python3

import os
import subprocess
from typing import Sequence


def _run(cmd: Sequence[str]) -> str:
    subprocess.check_call(("echo", "$", *cmd))
    proc = subprocess.run(cmd, stdout=subprocess.PIPE, check=True)
    return proc.stdout.decode().strip()


def main() -> int:

    cmd: Sequence[str]
    cmd = (
        *("dotnet", "tool", "update"),
        "--global",
        "coveralls.net",
    )
    _run(cmd)

    cmd = ("git", "show", "-s", "--format=%ae")
    git_email = _run(cmd)

    cmd = [
        "csmacnz.Coveralls",
        "--opencover",
        "--useRelativePaths",
        *("-i", "coverage/coverage.opencover.xml"),
        *("--commitAuthor", os.environ["BUILD_SOURCEVERSIONAUTHOR"]),
        *("--commitEmail", git_email),
        *("--commitMessage", os.environ["BUILD_SOURCEVERSIONMESSAGE"]),
        *("--jobId", os.environ["BUILD_BUILDID"]),
    ]

    is_pr = bool(os.environ.get("SYSTEM_PULLREQUEST_PULLREQUESTNUMBER", None))
    if is_pr:
        cmd.extend(("--commitBranch", os.environ["SYSTEM_PULLREQUEST_SOURCEBRANCH"]))
        cmd.extend(("--commitId", os.environ["SYSTEM_PULLREQUEST_SOURCECOMMITID"]))
        cmd.extend(
            ("--pullRequest", os.environ["SYSTEM_PULLREQUEST_PULLREQUESTNUMBER"])
        )
    else:
        source_branch = os.environ["BUILD_SOURCEBRANCH"]
        prefix = "refs/heads/"
        assert source_branch.startswith(prefix)
        cmd.extend(("--commitBranch", source_branch[len(prefix) :]))
        cmd.extend(("--commitId", os.environ["BUILD_SOURCEVERSION"]))

    _run(cmd)

    return 0


if __name__ == "__main__":
    exit(main())
