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
    C.add_tag_arg(parser)
    JC.add_common_args(parser)
    args, rest = parser.parse_known_args(argv)

    dist_tag_dir = C.DIST_DIR / args.tag

    stages = ["package"]

    if args.clean:
        stages.insert(0, "clean")
        if dist_tag_dir.is_dir():
            shutil.rmtree(dist_tag_dir)
    dist_tag_dir.mkdir(parents=True, exist_ok=True)

    cmd = (
        JC.mvn_exe(),
        "-ntp",
        *stages,
        "-DskipTests",
        "assembly:single@create-deployable",
        "-f", (C.PROJ_ROOT / "src/common/com.smi.microservices.parent").resolve(),
        *rest,
    )
    C.run(cmd)
    
    zips = {
        Path(x) for x in
        glob.glob(
            f"{C.PROJ_ROOT}/src/**/*deploy-distribution.zip",
            recursive=True,
        )
    }
    assert 1 == len(zips), "Expected 1 zip file (CTP)"
    for zip_path in zips:
        shutil.copyfile(
            zip_path,
            dist_tag_dir / f"{zip_path.name.split('-')[0]}-{args.tag}.zip",
        )

    C.create_checksums(dist_tag_dir, "ctp")

    return 0


if __name__ == "__main__":
    raise SystemExit(main())
