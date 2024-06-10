"""Common build variables and functions"""

import argparse
import hashlib
import functools
import os
import subprocess
from pathlib import Path
from typing import Dict
from typing import Optional
from typing import Sequence
from typing import Union

PROJ_ROOT = (Path(__file__).parent / "..").resolve()
DIST_DIR = PROJ_ROOT / "dist"
STR_LIKE = Union[str, Path]


def add_clean_arg(parser: argparse.ArgumentParser) -> None:    
    parser.add_argument(
        "--clean",
        action="store_true",
        help="Cleanup any existing files",
    )


def add_tag_arg(parser: argparse.ArgumentParser) -> None:    
    parser.add_argument(
        "tag",
        help="The git tag for the release",
    )


def run(
    cmd: Sequence[STR_LIKE],
    *,
    cwd: Optional[str] = None,
    env: Dict[str, str] = None,
) -> None:

    cmd = [str(x) for x in cmd]
    cwd = cwd or PROJ_ROOT
    if env is None:
        env = {}

    subprocess.check_call(("echo", "$", *cmd), cwd=cwd)
    subprocess.check_call(cmd, cwd=cwd, env={**os.environ, **env})


def create_checksums(dist_tag_dir: Path, product: str) -> None:
    file_checksums = {x.name: _md5sum(x) for x in dist_tag_dir.iterdir()}
    print("\n=== Checksums ===")
    with open(dist_tag_dir / f"MD5SUM-{product}.txt", "w") as md5_file:
        for file_name, md5sum in file_checksums.items():
            line = f"{md5sum} {file_name}\n"
            print(line, end="")
            md5_file.write(line)


def verify_md5(file_path: Path, expected_md5: str):
    actual_md5 = _md5sum(file_path)
    assert expected_md5 == actual_md5


def _md5sum(file_path: Path) -> str:
    with open(file_path, mode="rb") as f:
        d = hashlib.md5()
        for buf in iter(functools.partial(f.read, 128), b""):
            d.update(buf)
    return d.hexdigest()


def get_docker_parser() -> argparse.ArgumentParser:
    parser = argparse.ArgumentParser()
    parser.add_argument(
        "--podman",
        action="store_true",
        help="Use podman instead of docker",
    )
    return parser


def start_containers(
    compose_file: Path,
    *,
    env: Dict[str, str] = None,
    docker: str,
    checks: Sequence[str]
) -> None:

    user = ("--user", f"{os.geteuid()}:{os.getegid()}") if docker == "docker" else ()
    volume = f"-v{compose_file.parent}:/run"
    if docker == "podman":
        volume += ":z"

    cmd = (
        docker, "run",
        "--rm",
        volume,
        *user,
        "safewaters/docker-lock",
        *(
            "lock", "rewrite",
            "--lockfile-name", f"{compose_file.name}.lock"
        )
    )
    run(cmd)

    with open(compose_file) as f:
        print(f.read())

    cmd = (
        f"{docker}-compose",
        "-f", compose_file,
        "up",
        "--detach",
        "--force-recreate",
    )
    run(cmd, env=env)

    cmd = (docker, "ps")
    run(cmd)

    for c in checks:
        cmd = (
            "./bin/wait-for.bash",
            "--timeout", "60s",
            f"{docker} exec {c}",
        )
        try:
            run(cmd)
        except subprocess.CalledProcessError:
            # NOTE(rkm 2022-03-01) Print container logs when a check fails
            cmd = (
                f"{docker}-compose",
                "-f", compose_file,
                "logs"
            )
            run(cmd)
            raise
