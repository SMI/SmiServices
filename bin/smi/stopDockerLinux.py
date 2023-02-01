#!/usr/bin/env python3

import os
import platform
import sys

sys.path.append(os.path.join(os.path.dirname(__file__), '..'))
import common as C

_PLATFORM = "-arm" if platform.processor() == "arm" else ""
_COMPOSE_FILE_NAME = f"linux-dotnet{_PLAT}.yml"
_COMPOSE_FILE_PATH = (C.PROJ_ROOT / "utils/docker-compose" / _COMPOSE_FILE_NAME).resolve()
assert _COMPOSE_FILE_PATH.is_file()


def main() -> int:

    parser = C.get_docker_parser()
    args = parser.parse_args()

    docker = "docker" if not args.podman else "podman"

    cmd = (
        f"{docker}-compose",
        "-f", _COMPOSE_FILE_PATH,
        "down",
        "--timeout", 0,
    )
    C.run(cmd)

    return 0


if __name__ == "__main__":
    raise SystemExit(main())
