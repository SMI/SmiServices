#!/usr/bin/env python3
import os
import sys

import java_common

sys.path.append(os.path.join(os.path.dirname(__file__), ".."))
import common as c  # noqa: E402

_CTP_LIB_DIR = f"{c.PROJ_ROOT}/lib/ctp"
assert os.path.isdir(_CTP_LIB_DIR), "Could not find CTP libs dir"

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
        java_common.mvn_exe(),
        "--quiet",
        "--no-transfer-progress",
        "install:install-file",
    )

    base_jar_cmd = (
        *base_cmd,
        "-DgroupId=dat",
        "-Dpackaging=jar",
        "-DgeneratePom=true",
    )
    for j in _CTP_JARS:
        c.run(
            (
                *base_jar_cmd,
                f"-DartifactId={j}",
                "-Dversion=1.0",
                f"-Dfile={j}.jar",
            ),
            cwd=_CTP_LIB_DIR,
        )
    for j in _DAT_JARS:
        c.run(
            (
                *base_jar_cmd,
                f"-DartifactId={j}",
                "-Dversion=1.1",
                f"-Dfile={j}.jar",
            ),
            cwd=_CTP_LIB_DIR,
        )

    c.run(
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
