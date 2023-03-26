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

import javaCommon as JC
import installLibs as L


def main(argv: Optional[Sequence[str]] = None) -> int:

    parser = argparse.ArgumentParser()
    C.add_clean_arg(parser)
    JC.add_common_args(parser)
    args, rest = parser.parse_known_args(argv)

    if args.install_libs:
        rc = L.main()
        if rc:
            return rc

    stages = ["compile"]
    if args.clean:
        stages.insert(0, "clean")

    cmd = (
        JC.mvn_exe(),
        "-ntp",
        *stages,
        "-f", (C.PROJ_ROOT / "src/common/com.smi.microservices.parent").resolve(),
        *rest,
    )
    C.run(cmd)

    return 0


if __name__ == "__main__":
    raise SystemExit(main())
