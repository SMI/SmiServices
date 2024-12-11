"""Common build variables and functions"""

import argparse
import functools
import hashlib
import os
import shlex
import subprocess
from collections.abc import Sequence

PROJ_ROOT = os.path.realpath(os.path.join(os.path.dirname(__file__), ".."))
DIST_DIR = f"{PROJ_ROOT}/dist"


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
    cmd: Sequence[str],
    *,
    cwd: str | None = None,
    env: dict[str, str] | None = None,
) -> None:

    cwd = cwd or PROJ_ROOT
    if env is None:
        env = {}

    print(shlex.join(("$", *cmd)))
    subprocess.check_call(cmd, cwd=cwd, env={**os.environ, **env})


def create_checksums(dist_tag_dir: str, product: str) -> None:
    file_checksums = {
        x: _md5sum(f"{dist_tag_dir}/{x}") for x in os.listdir(dist_tag_dir)
    }
    print("\n=== Checksums ===")
    with open(f"{dist_tag_dir}/MD5SUM-{product}.txt", "w") as md5_file:
        for file_name, md5sum in file_checksums.items():
            line = f"{md5sum} {file_name}\n"
            print(line, end="")
            md5_file.write(line)


def verify_md5(file_path: str, expected_md5: str) -> None:
    actual_md5 = _md5sum(file_path)
    assert (
        expected_md5 == actual_md5
    ), f"Checksum mismatch: expected={expected_md5}, actual={actual_md5}"


def _md5sum(file_path: str) -> str:
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
    compose_file: str,
    *,
    env: dict[str, str] | None = None,
    docker: str,
    checks: Sequence[str],
) -> None:

    user = ("--user", f"{os.geteuid()}:{os.getegid()}") if docker == "docker" else ()
    volume = f"-v{os.path.dirname(compose_file)}:/run"
    if docker == "podman":
        volume += ":z"

    cmd = (
        docker,
        "run",
        "--rm",
        volume,
        *user,
        "safewaters/docker-lock",
        *(
            "lock",
            "rewrite",
            "--lockfile-name",
            f"{os.path.basename(compose_file)}.lock",
        ),
    )
    run(cmd)

    with open(compose_file) as f:
        print(f.read())

    cmd = (
        f"{docker}",
        "compose",
        "-f",
        compose_file,
        "up",
        "--quiet-pull",
        "--detach",
        "--force-recreate",
    )
    run(cmd, env=env)

    cmd = (docker, "ps")
    run(cmd)

    for c in checks:
        cmd = (
            "./bin/wait-for.bash",
            "--timeout",
            "60s",
            f"{docker} exec {c}",
        )
        try:
            run(cmd)
        except subprocess.CalledProcessError:
            # NOTE(rkm 2022-03-01) Print container logs when a check fails
            cmd = (
                f"{docker}",
                "compose",
                "-f",
                compose_file,
                "logs",
            )
            run(cmd)
            raise


def is_ci() -> bool:
    ci = os.environ.get("CI", None)
    if ci is None:
        return False
    return ci == "1" or ci.lower() == "true"
