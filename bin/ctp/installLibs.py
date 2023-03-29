#!/usr/bin/env python3

import os
import sys
from pathlib import Path

sys.path.append(os.path.join(os.path.dirname(__file__), '..'))
import common as C

import javaCommon as JC


_CTP_LIB_DIR = Path(f"{C.PROJ_ROOT}/lib/ctp")
assert _CTP_LIB_DIR.is_dir()

_CTP_JARS = [
    "CTP",
    "dcm4che",
    "util",
    "log4j",
    "pixelmed_codec",
    "dcm4che-imageio-rle-2.0.25",
]

_DAT_JARS = [
    "clibwrapper_jiio",
    "jai_imageio",
]


def main() -> int:

    base_cmd = (
        JC.mvn_exe(),
        "--quiet", "--no-transfer-progress",
        "install:install-file"
    )

    base_jar_cmd = (
        *base_cmd,
        "-DgroupId=dat",
        "-Dpackaging=jar",
        "-DgeneratePom=true",
    )
    for j in _CTP_JARS:
        C.run(
            (
                *base_jar_cmd,
                f"-DartifactId={j}",
                "-Dversion=1.0",
                f"-Dfile={j}.jar",
            ),
            cwd=_CTP_LIB_DIR,
        )
    for j in _DAT_JARS:
        C.run(
            (
                *base_jar_cmd,
                f"-DartifactId={j}",
                "-Dversion=1.1",
                f"-Dfile={j}.jar",
            ),
            cwd=_CTP_LIB_DIR,
        )

    C.run(
        (
            *base_cmd,
            "-Dfile=DAT.jar",
            "-DpomFile=pom.xml",
        ),
        cwd=_CTP_LIB_DIR,
    )

    return 0


if __name__ == "__main__":
    raise SystemExit(main())
