#!/usr/bin/env python3
"""
Builds compiled packages for the C#, Java, and Python services in this repo.
"""
import argparse
import functools
import glob
import hashlib
import re
import shutil
import subprocess
import sys
from pathlib import Path
from typing import Optional
from typing import Sequence
from typing import Tuple
from typing import Union


_LINUX = "linux"
_WINDOWS = "win"
_PLATFORMS = (_LINUX, _WINDOWS)
_STR_LIKE = Union[str, Path]
_ASSEMBLY_NAME_RE = re.compile(".*AssemblyName>(.*)<", re.IGNORECASE)
_IS_PUBLISHABLE_RE = re.compile(".*IsPublishable>false<", re.IGNORECASE)
_PYTHON_DIR = Path("src/common/Smi_Common_Python")


def _run(cmd: Sequence[_STR_LIKE]) -> None:
    subprocess.check_call(("echo", "$", *cmd))
    subprocess.check_call(cmd)


def _windows_bash_fixup(platform: str, cmd: Sequence[_STR_LIKE]) -> Sequence[_STR_LIKE]:
    return cmd if platform != _WINDOWS else ("powershell", "bash", *cmd)


def _build_java_packages(dist_tag_dir: Path, tag: str) -> None:

    # Build Java microserves

    cmd: Tuple[str, ...]
    cmd = ("utils/install-ctp.bash",)
    _run(cmd)

    cmd = (
        "mvn",
        "-ntp",
        "-f", "./src/common/com.smi.microservices.parent",
        "-DskipTests",
        "package",
        "assembly:single@create-deployable",
    )
    _run(cmd)

    zips = {
        Path(x) for x in glob.glob("./src/**/*deploy-distribution.zip", recursive=True)
    }
    assert 1 == len(zips), "Expected 1 zip file (CTP)"
    for zip_path in zips:
        shutil.copyfile(
            zip_path,
            dist_tag_dir / f"{zip_path.name.split('-')[0]}-{tag}.zip",
        )

    # Build nerd

    cmd = (
        "mvn",
        "-ntp",
        "-f", "./src/microservices/uk.ac.dundee.hic.nerd",
        "-DskipTests",
        "package",
    )
    _run(cmd)

    (nerd_jar,) = {
        Path(x)
        for x in glob.glob(
            "./src/microservices/uk.ac.dundee.hic.nerd/target/nerd-*.jar",
            recursive=True,
        )
    }
    shutil.copyfile(
        nerd_jar,
        dist_tag_dir / f"smi-nerd-{tag}.jar",
    )


def _build_python_package(dist_tag_dir: Path, python_exe: Path) -> None:

    cmd = (
        python_exe,
        "-m", "pip",
        "install",
        "-r", _PYTHON_DIR / "requirements.txt",
    )
    _run(cmd)

    cmd = (
        python_exe,
        _PYTHON_DIR / "setup.py",
        "bdist_wheel",
        "-d", dist_tag_dir,
    )
    _run(cmd)


def _md5sum(file_path: Path) -> str:
    with open(file_path, mode="rb") as f:
        d = hashlib.md5()
        for buf in iter(functools.partial(f.read, 128), b""):
            d.update(buf)
    return d.hexdigest()


def main(argv: Optional[Sequence[str]] = None) -> int:

    parser = argparse.ArgumentParser()
    parser.add_argument(
        "platform",
        type=str.lower,
        choices=_PLATFORMS,
        help="The platform to build for",
    )
    parser.add_argument("tag", help="The git tag for the release")
    parser.add_argument(
        "python_build_exe",
        type=Path,
        help="Path to the python  exe to use for the build",
    )
    args = parser.parse_args(argv)

    tag = args.tag
    platform = args.platform
    dist_tag_dir = Path("dist", tag)

    if dist_tag_dir.is_dir():
        print(f"Error: {dist_tag_dir} already exists", file=sys.stderr)
        return 1
    dist_tag_dir.mkdir(parents=True)

    python_exe = args.python_build_exe
    if not python_exe.is_file():
        print(f"Error: {python_exe} not found", file=sys.stderr)
        return 1

    cmd: Sequence[_STR_LIKE]

    # Build dotnet projects

    cmd = ("utils/dotnet-build.bash",)
    cmd = _windows_bash_fixup(platform, cmd)
    _run(cmd)

    # Publish dotnet packages

    smi_services_output_dir = f"smi-services-{tag}-{platform}-x64"
    cmd = (
        "dotnet", "publish",
        "-p:Platform=x64",
        "--configuration", "Release",
        "-p:PublishTrimmed=false",
        "--runtime", f"{platform}-x64",
        "--output", str(dist_tag_dir / smi_services_output_dir),
        "--nologo",
    )
    _run(cmd)

    if platform == _LINUX:
        cmd = (
            "tar",
            "-C", dist_tag_dir,
            "-czf",
            dist_tag_dir / f"{smi_services_output_dir}.tgz",
            smi_services_output_dir,
        )
    elif platform == _WINDOWS:
        # NOTE(rkm 2020-12-23) If building Windows _from_ Linux, this needs to be 7za
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

    _run(cmd)
    shutil.rmtree(dist_tag_dir / smi_services_output_dir)

    # Build Java and Python packages
    # These don't need separate packages for each OS - create on Linux only
    if platform == _LINUX:
        _build_java_packages(dist_tag_dir, tag)
        _build_python_package(dist_tag_dir, python_exe)

    # Create checksum files
    file_checksums = {x.name: _md5sum(x) for x in dist_tag_dir.iterdir()}
    with open(dist_tag_dir / f"MD5SUMS-{platform}.txt", "w") as md5_file:
        for file_name, md5sum in file_checksums.items():
            md5_file.write(f"{md5sum} {file_name}\n")

    return 0


if __name__ == "__main__":
    exit(main())
