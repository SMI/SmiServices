#!/usr/bin/env python3
import argparse
import os
import shutil
import sys
from collections.abc import Sequence

import dotnet_common

sys.path.append(os.path.join(os.path.dirname(__file__), ".."))
import common  # noqa: E402

_LINUX = "linux"
_WINDOWS = "win"


def runtime_platform() -> str:
    if os.name == "posix":
        return _LINUX
    elif os.name == "nt":
        return _WINDOWS
    raise ValueError(os.name)


def main(argv: Sequence[str] | None = None) -> int:

    parser = argparse.ArgumentParser()
    common.add_clean_arg(parser)
    common.add_tag_arg(parser)
    dotnet_common.add_args(parser)
    parser.add_argument(
        "--no-build",
        action="store_true",
    )
    args = parser.parse_args(argv)

    dist_tag_dir = f"{common.DIST_DIR}/{args.tag}"

    if args.clean and os.path.isdir(dist_tag_dir):
        shutil.rmtree(dist_tag_dir)
    os.makedirs(dist_tag_dir, exist_ok=True)

    platform = runtime_platform()
    rid = f"{platform}-x64"
    smi_services_output_dir = f"smi-services-{args.tag}-{rid}"

    cmd: tuple[str, ...] = (
        "dotnet",
        "publish",
        "-warnaserror",
        "--use-current-runtime",
        "--configuration",
        args.configuration,
        "--no-build" if args.no_build else "",
        "-p:PublishTrimmed=false",
        "--self-contained",
        "--output",
        f"{dist_tag_dir}/{smi_services_output_dir}",
        "--verbosity",
        "quiet",
        "--nologo",
        "src/SmiServices",
    )
    common.run(cmd)

    if platform == _LINUX:
        cmd = (
            "tar",
            "-C",
            dist_tag_dir,
            "-czf",
            f"{dist_tag_dir}/{smi_services_output_dir}.tgz",
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
            f"{dist_tag_dir}/{smi_services_output_dir}.zip",
            smi_services_output_dir,
        )
    else:
        print(f"Error: No case for platform {platform}", file=sys.stderr)
        return 1

    common.run(cmd)
    shutil.rmtree(f"{dist_tag_dir}/{smi_services_output_dir}")

    common.create_checksums(dist_tag_dir, "smiservices")

    return 0


if __name__ == "__main__":
    raise SystemExit(main())
