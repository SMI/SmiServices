#!/usr/bin/env python3
import os
import sys

sys.path.append(os.path.join(os.path.dirname(__file__), ".."))
import common  # noqa: E402

_COMPOSE_FILE_NAME = "linux-java.yml"
_COMPOSE_FILE_PATH = f"{common.PROJ_ROOT}/utils/docker-compose/{_COMPOSE_FILE_NAME}"
assert os.path.isfile(
    _COMPOSE_FILE_PATH,
), f"Compose file does not exist: {_COMPOSE_FILE_PATH}"


def main() -> int:

    parser = common.get_docker_parser()
    args = parser.parse_args()

    docker = "docker" if not args.podman else "podman"

    cmd = (
        f"{docker}-compose",
        "-f",
        _COMPOSE_FILE_PATH,
        "down",
        "--timeout",
        "0",
    )
    common.run(cmd)

    return 0


if __name__ == "__main__":
    raise SystemExit(main())
