#!/usr/bin/env python3
import argparse
import glob
import os
import shutil
import sys
from collections.abc import Sequence

import java_common

sys.path.append(os.path.join(os.path.dirname(__file__), ".."))
import common  # noqa: E402


def main(argv: Sequence[str] | None = None) -> int:

    parser = argparse.ArgumentParser()
    common.add_clean_arg(parser)
    common.add_tag_arg(parser)
    java_common.add_common_args(parser)
    args, rest = parser.parse_known_args(argv)

    dist_tag_dir = f"{common.DIST_DIR}/{args.tag}"

    stages = ["package"]

    if args.clean:
        stages.insert(0, "clean")
        if os.path.isdir(dist_tag_dir):
            shutil.rmtree(dist_tag_dir)
    os.makedirs(dist_tag_dir, exist_ok=True)

    cmd = (
        java_common.mvn_exe(),
        "-ntp",
        *stages,
        "-DskipTests",
        "assembly:single@create-deployable",
        "-f",
        f"{common.PROJ_ROOT}/src/common/com.smi.microservices.parent",
        *rest,
    )
    common.run(cmd)

    zips = {
        x
        for x in glob.glob(
            f"{common.PROJ_ROOT}/src/**/*deploy-distribution.zip",
            recursive=True,
        )
    }
    assert 1 == len(zips), "Expected 1 zip file (CTP)"
    for zip_path in zips:
        shutil.copyfile(
            zip_path,
            f"{dist_tag_dir}/{zip_path.split('-')[0]}-{args.tag}.zip",
        )

    common.create_checksums(dist_tag_dir, "ctp")

    return 0


if __name__ == "__main__":
    raise SystemExit(main())
