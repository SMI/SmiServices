#!/usr/bin/env python3

import argparse
import glob
import os
import shutil
import sys
from pathlib import Path
from typing import Optional
from typing import Sequence

sys.path.append(os.path.join(os.path.dirname(__file__), '..'))
import common as C

import build as JB
import installLibs as L
import javaCommon as JC


def main(argv: Optional[Sequence[str]] = None) -> int:

    parser = argparse.ArgumentParser()
    C.add_clean_arg(parser)
    JC.add_common_args(parser)
    args, rest = parser.parse_known_args(argv)

    stages = ["test"]
    if args.clean:
        stages.insert(0, "clean")

    cmd = (
        JC.mvn_exe(),
        "-ntp",
        "-DtrimStackTrace=false",
        *stages,
        "-f", (C.PROJ_ROOT / "src/common/com.smi.microservices.parent").resolve(),
        *rest,
    )
    C.run(cmd)

    return 0


if __name__ == "__main__":
    raise SystemExit(main())
