#!/usr/bin/env python3
import argparse
import os
import sys
from collections.abc import Sequence

import install_libs
import java_common

sys.path.append(os.path.join(os.path.dirname(__file__), ".."))
import common  # noqa: E402


def main(argv: Sequence[str] | None = None) -> int:

    parser = argparse.ArgumentParser()
    common.add_clean_arg(parser)
    java_common.add_common_args(parser)
    args, rest = parser.parse_known_args(argv)

    if args.install_libs:
        rc = install_libs.main()
        if rc:
            return rc

    stages = ["test"]
    if args.clean:
        stages.insert(0, "clean")

    cmd = (
        java_common.mvn_exe(),
        "-ntp",
        "-DtrimStackTrace=false",
        *stages,
        "-f",
        f"{common.PROJ_ROOT}/src/common/com.smi.microservices.parent",
        *rest,
    )
    common.run(cmd)

    return 0


if __name__ == "__main__":
    raise SystemExit(main())
