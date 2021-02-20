#!/usr/bin/env python3
"""
Builds compiled packages for the C# and Java services in this repo.

The build output for the C# services looks like:
dist/
  v1.2.3/
    # Temporary during build. Contains all C# services and DLLs separated by csproj
    smi-services-build-tmp/
      DicomTagReader/
        ...

    # Temporary during build. Contains all C# services and DLLs merged into one dir
    smi-services-{tag}-{platform}-x64/
      ...

    # The final output archive
    smi-services-{tag}-{platform}-x64.tgz

The files in the merged dir are checked for accidental overwriting, which can occur when
publishing a solution of multiple projects into a single directory. See
https://github.com/dotnet/sdk/issues/9984.
"""
import argparse
import concurrent
import filecmp
import functools
import glob
import hashlib
import re
import shutil
import subprocess
import sys
from concurrent.futures import ThreadPoolExecutor
from pathlib import Path
from typing import Dict
from typing import Optional
from typing import Sequence
from typing import Union


_LINUX = "linux"
_WINDOWS = "win"
_PLATFORMS = (_LINUX, _WINDOWS)
_STR_LIKE = Union[str, Path]
_ASSEMBLY_NAME_RE = re.compile(".*AssemblyName>(.*)<", re.IGNORECASE)
_IS_PUBLISHABLE_RE = re.compile(".*IsPublishable>false<", re.IGNORECASE)


def _run(cmd: Sequence[_STR_LIKE]) -> None:
    subprocess.check_call(("echo", *cmd))
    subprocess.check_call(cmd)


def _windows_bash_fixup(platform: str, cmd: Sequence[_STR_LIKE]) -> Sequence[_STR_LIKE]:
    return cmd if platform != _WINDOWS else ("powershell", "bash", *cmd)


def _build_java_packages(dist_tag_dir: Path, tag: str) -> None:

    # Build Java microserves

    cmd = (".azure-pipelines/scripts/install-ctp.bash",)
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
    assert 2 == len(zips), "Expected 2 zip files (CTP and ExtractorCLI)"
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


def _build_csproj(build_dir: Path, platform: str, csproj_path: Path) -> None:

    assembly_name: Optional[str] = None
    with open(csproj_path) as f:
        for line in f:

            pub_match = _IS_PUBLISHABLE_RE.match(line)
            if pub_match:
                print(f"{cproj_path} not publishable")
                return None

            aname_match = _ASSEMBLY_NAME_RE.match(line)
            if aname_match:
                assembly_name = aname_match.group(1)
                break

    if not assembly_name:
        raise AssertionError(f"Couldn't find AssemblyName in {csproj_path}")

    publish_dir = build_dir / assembly_name
    publish_dir.mkdir()
    cmd = (
        "dotnet", "publish",
        "-p:Platform=x64",
        "--configuration", "Release",
        "-p:PublishTrimmed=false",
        "--runtime", f"{platform}-x64",
        "--output", publish_dir,
        "--nologo",
        csproj_path,
    )

    proc = subprocess.run(
        cmd,
        stdout=subprocess.PIPE,
        stderr=subprocess.PIPE,
    )
    # TODO(rkm 2021-02-20) Runs in parallel so need to capture output and log properly
    stdout = proc.stdout.decode().strip()
    stderr = proc.stderr.decode().strip()
    if stdout or stderr:
        print(f"=== {csproj_path} ===")
        print(f"STDOUT\n{stdout}\n")
        print(f"STDERR\n{stderr}\n")

    if proc.returncode:
        raise subprocess.CalledProcessError(f"Build failed for {csproj_path}")

    return None


def _md5sum(file_path: Path) -> str:
    with open(file_path, mode="rb") as f:
        d = hashlib.md5()
        for buf in iter(functools.partial(f.read, 128), b""):
            d.update(buf)
    return d.hexdigest()


def _merge_files(build_dir: Path, base_output_dir: Path) -> bool:

    base_output_dir.mkdir()
    files = {}
    clobbered = set()

    def _check_clobber_and_copy(src: Path, output_dir=None) -> None:

        output_dir = output_dir or base_output_dir

        if src.is_dir():
            sub_dir = output_dir / src.name
            sub_dir.mkdir(exist_ok=True)
            for f in src.iterdir():
                _check_clobber_and_copy(f, sub_dir)
            return None

        nonlocal files, clobbered
        existing = output_dir / src.name
        if existing.is_file() and not filecmp.cmp(
            existing,
            src,
            # NOTE(rkm 2021-02-20) Don't just compare on os.stat
            shallow=False,
        ):
            clobbered.add(existing)

        if not existing in files:
            files[existing] = []
        files[existing].append(src)

        shutil.copy2(src, output_dir)
        return None

    for csproj_dir in [d for d in build_dir.iterdir() if d.is_dir()]:
        for file_or_dir_path in csproj_dir.iterdir():
            _check_clobber_and_copy(file_or_dir_path)

    for file_path in sorted(clobbered):
        print(f"=== Clobbered {file_path.name} ===")
        for f in files[file_path]:
            print(f"{_md5sum(f)}\t{f}")
        print()
    else:
        return False

    return True


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
    dist_tag_dir = Path("dist", tag)

    if dist_tag_dir.is_dir():
        print(f"Error: {dist_tag_dir} already exists", file=sys.stderr)
        return 1
    dist_tag_dir.mkdir(parents=True)

    cmd: Sequence[_STR_LIKE]

    # Build dotnet projects

    cmd = (".azure-pipelines/scripts/dotnet-build.bash",)
    cmd = _windows_bash_fixup(platform, cmd)
    _run(cmd)

    # Publish dotnet packages

    tmp_build_dir = dist_tag_dir / "smi-services-build-tmp"
    tmp_build_dir.mkdir()
    build_csproj = functools.partial(_build_csproj, tmp_build_dir, platform)
    csproj_paths = {Path(x) for x in glob.glob("src/**/*.csproj", recursive=True)}
    build_failed = False

    # NOTE(rkm 2021-02-20) Might get a bit of a benefit here - runs on Standard_DS2_v2 (2 vCPU)
    with ThreadPoolExecutor() as executor:
        build_results = {executor.submit(build_csproj, p): p for p in csproj_paths}
        for future in concurrent.futures.as_completed(build_results):
            csproj_path = build_results[future]
            try:
                future.result()
            except Exception as exc:
                print(f"{csproj_path} generated an exception: {exc}")
                build_failed = True

    if build_failed:
        print("A build failed - exiting")
        return 1

    smi_services_output_dir = f"smi-services-{tag}-{platform}-x64"
    did_clobber = _merge_files(tmp_build_dir, dist_tag_dir / smi_services_output_dir)
    if did_clobber:
        return 1

    shutil.rmtree(tmp_build_dir)

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

    if platform == _LINUX and False:
        _build_java_packages(dist_tag_dir, tag)

    # Create checksum files
    file_checksums = {x.name: _md5sum(x) for x in dist_tag_dir.iterdir()}
    with open(dist_tag_dir / f"MD5SUMS-{platform}.txt", "w") as md5_file:
        for file_name, md5sum in file_checksums.items():
            md5_file.write(f"{md5sum} {file_name}\n")

    return 0


if __name__ == "__main__":
    exit(main())
