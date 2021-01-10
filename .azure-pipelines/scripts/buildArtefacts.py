#!/usr/bin/env python3
import argparse
import glob
import hashlib
import shutil
import subprocess
import sys
from pathlib import Path
from typing import Dict
from typing import Optional
from typing import Sequence
from typing import Union


_LINUX = "linux"
_WINDOWS = "win"
_PLATFORMS = (_LINUX, _WINDOWS)
_STR_LIKE = Union[str, Path]


def _run(cmd: Sequence[_STR_LIKE]) -> None:
    subprocess.check_call(("echo", *cmd))
    subprocess.check_call(cmd)


def main(argv: Optional[Sequence[str]] = None) -> int:

    parser = argparse.ArgumentParser()
    parser.add_argument(
        "platform",
        type=str.lower,
        choices=_PLATFORMS,
        help="The platform to build for",
    )
    parser.add_argument("tag", help="The git tag for the release")
    args = parser.parse_args(argv)

    tag = args.tag
    platform = args.platform
    dist_dir = Path("dist", tag)
    smi_services_output_dir = f"smi-services-{tag}-{platform}-x64"

    if dist_dir.is_dir():
        print(f"Error: {dist_dir} already exists", file=sys.stderr)
        return 1
    dist_dir.mkdir(parents=True)

    cmd: Sequence[_STR_LIKE]

    # Build dotnet projects

    cmd = (
        ".azure-pipelines/scripts/dotnet-build.bash",
    )
    _run(cmd)

    # Publish dotnet packages

    cmd = (
        "dotnet", "publish",
        "-p:Platform=x64",
        "--configuration", "Release",
        "-p:PublishTrimmed=false",
        "--runtime", f"{platform}-x64",
        "--output", dist_dir / smi_services_output_dir,
        "-v", "quiet", "--nologo",
    )
    _run(cmd)

    if platform == _LINUX:
        cmd = (
            "tar", "-c",
            # TODO(rkm 2020-12-23) pigz
            "-z",  # "-I", shlex.quote("pigz -9"),
            "-f", dist_dir / f"{smi_services_output_dir}.tgz",
            dist_dir / smi_services_output_dir,
        )
    elif platform == _WINDOWS:
        # NOTE(rkm 2020-12-23) If building Windows _from_ Linux, this needs to be 7za
        cmd = (
            "7z", "a",
            "-tzip",
            "-mx9",
            dist_dir / f"{smi_services_output_dir}.zip",
            "-r", dist_dir / smi_services_output_dir,
        )
    else:
        print(f"Error: No case for platform {platform}", file=sys.stderr)
        return 1

    _run(cmd)
    shutil.rmtree(dist_dir / smi_services_output_dir)

    # Build Java microserves

    cmd = (
        "mvn", "-ntp", "-q",
        "-f", "./src/common/com.smi.microservices.parent",
        "-DskipTests",
        "package",
        "assembly:single@create-deployable",
    )
    _run(cmd)

    zips = {
        Path(x) for x in glob.glob("./src/**/*deploy-distribution.zip", recursive=True)
    }
    assert 2 == len(zips), "Expected 2 zip files (CTP and ExtractorCLI)"
    for zip_path in zips:
        shutil.copyfile(
            zip_path,
            dist_dir / f"{zip_path.name.split('-')[0]}-{tag}.zip",
        )

    # Build nerd

    cmd = (
        "mvn", "-ntp", "-q",
        "-f", "./src/microservices/uk.ac.dundee.hic.nerd",
        "-DskipTests",
        "package",
    )
    _run(cmd)

    nerd_jar, = {
        Path(x)
        for x in glob.glob(
            "./src/microservices/uk.ac.dundee.hic.nerd/target/nerd-*.jar",
            recursive=True,
        )
    }
    shutil.copyfile(
        nerd_jar,
        dist_dir / f"smi-nerd-{tag}.jar",
    )

    # Create checksum file
    # NOTE(rkm 2020-12-23) No easy cross-platform md5sum tool, so have to use hashlib

    hashes: Dict[str, str] = {}
    for artefact_path in dist_dir.iterdir():
        with open(artefact_path, "rb") as artefact_file:
            hashes[artefact_path.name] = hashlib.md5(artefact_file.read()).hexdigest()
    with open(dist_dir / f"MD5SUMS-{platform}.txt", "w") as md5_file:
        for filename in sorted(hashes):
            md5_file.write(f"{hashes[filename]} {filename}\n")

    return 0


if __name__ == "__main__":
    exit(main())
