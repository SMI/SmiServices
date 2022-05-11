#!/usr/bin/env python3

import glob
import os
import shutil
import sys
from pathlib import Path

sys.path.append(os.path.join(os.path.dirname(__file__), '..'))
import common as C

_NERD_TARGET_DIR = C.PROJ_ROOT / "src/microservices/uk.ac.dundee.hic.nerd/target"


def main() -> int:

    args, rest = C.get_parser().parse_known_args()

    dist_tag_dir = C.DIST_DIR / args.tag

    if args.clean:
        shutil.rmtree(dist_tag_dir)
    dist_tag_dir.mkdir(parents=True, exist_ok=True)

    mvn = "mvn" if os.name == "posix" else "mvn.cmd"
    cmd = (
        mvn,
        "--no-transfer-progress",
        "package",
        "-f", "./src/microservices/uk.ac.dundee.hic.nerd",
        *rest,
    )
    C.run(cmd)

    jars = [
        Path(x)
        for x in glob.glob(f"{_NERD_TARGET_DIR}/nerd-*.jar", recursive=True)
    ]
    assert 1 == len(jars), "Expected 1 zip file (CTP)"
    shutil.copyfile(
        jars[0],
        dist_tag_dir / f"smi-nerd-{args.tag}.jar",
    )

    C.create_checksums(dist_tag_dir, "nerd")

    return 0


if __name__ == "__main__":
    raise SystemExit(main())
