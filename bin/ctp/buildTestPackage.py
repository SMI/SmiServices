#!/usr/bin/env python3

import glob
import os
import shutil
import sys
from pathlib import Path

sys.path.append(os.path.join(os.path.dirname(__file__), '..'))
import common as C

import installLibs as L


def main() -> int:

    parser = C.get_parser()
    parser.add_argument(
        "--install-libs",
        action="store_true",
        help="Install the CTP libraries only if specified"
    )
    args, rest = parser.parse_known_args()

    dist_tag_dir = C.DIST_DIR / args.tag

    if args.clean:
        shutil.rmtree(dist_tag_dir)
    dist_tag_dir.mkdir(parents=True, exist_ok=True)

    if args.install_libs:
        L.main()

    mvn = "mvn" if os.name == "posix" else "mvn.cmd"
    cmd = (
        mvn,
        "-ntp",
        "-DtrimStackTrace=false",
        "package",
        "assembly:single@create-deployable",
        "-f", (C.PROJ_ROOT / "src/common/com.smi.microservices.parent").resolve(),
        *rest,
    )
    C.run(cmd)

    zips = {
        Path(x) for x in glob.glob(f"{C.PROJ_ROOT}/src/**/*deploy-distribution.zip", recursive=True)
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
