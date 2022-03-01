#!/usr/bin/env python3

import os
import sys

sys.path.append(os.path.join(os.path.dirname(__file__), '..'))
import common as C

_COMPOSE_FILE_NAME = "linux-java.yml"
_COMPOSE_FILE_PATH = (C.PROJ_ROOT / "utils/docker-compose" / _COMPOSE_FILE_NAME).resolve()
assert _COMPOSE_FILE_PATH.is_file()


def main() -> int:

    parser = C.get_docker_parser()
    args = parser.parse_args()

    docker = "podman" if args.podman else "docker"

    C.start_containers(
        _COMPOSE_FILE_PATH,
        docker=docker,
        checks=(
           "rabbitmq rabbitmq-diagnostics -q ping",
        ),
    )

    return 0


if __name__ == "__main__":
    raise SystemExit(main())
