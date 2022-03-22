#!/usr/bin/env python3

import os
import shutil
import sys
from typing import Optional
from typing import Sequence

sys.path.append(os.path.join(os.path.dirname(__file__), '..'))
import common as C

import dotnetCommon as DC

_LINUX = "linux"
_WINDOWS = "win"


def runtime_platform() -> str:
    if os.name == "posix":
        return _LINUX
    elif os.name == "nt":
        return _WINDOWS
    raise ValueError(os.name)


def main(argv: Optional[Sequence[str]] = None) -> int:

    parser = C.get_parser()
    DC.add_args(parser)
    parser.add_argument(
        "--no-build",
        action="store_true",
    )
    args = parser.parse_args(argv)

    dist_tag_dir = C.DIST_DIR / args.tag

    if args.clean and dist_tag_dir.is_dir():
        shutil.rmtree(dist_tag_dir)
    dist_tag_dir.mkdir(parents=True, exist_ok=True)

    platform = runtime_platform()
    rid = f"{platform}-x64"
    smi_services_output_dir = f"smi-services-{args.tag}-{rid}"

    cmd = (
        "dotnet",
        "publish",
        "--use-current-runtime",
        "--configuration", args.configuration,
        "--no-build" if args.no_build else "",
        "-p:PublishTrimmed=false",
        "--self-contained",
        "--output", dist_tag_dir / smi_services_output_dir,
        "--verbosity", "quiet",
        "--nologo",
    )
    C.run(cmd)

    if platform == _LINUX:
        cmd = (
            "tar",
            "-C", dist_tag_dir,
            "-czf",
            dist_tag_dir / f"{smi_services_output_dir}.tgz",
            smi_services_output_dir,
        )
    elif platform == _WINDOWS:
        # TODO(rkm 2020-12-23) If building Windows _from_ Linux, this needs to be 7za
        cmd = (
            "7z",
            "a",
            f"-w{dist_tag_dir}",
            "-tzip",
            "-mx9",
            "-r",
            dist_tag_dir / f"{smi_services_output_dir}.zip",
            smi_services_output_dir,
        )
    else:
        print(f"Error: No case for platform {platform}", file=sys.stderr)
        return 1

    C.run(cmd)
    shutil.rmtree(dist_tag_dir / smi_services_output_dir)

    C.create_checksums(dist_tag_dir, "smiservices")

    return 0


if __name__ == "__main__":
    raise SystemExit(main())
