#!/usr/bin/env python3

import os
import sys

sys.path.append(os.path.join(os.path.dirname(__file__), '..'))
import common as C

_COMPOSE_FILE_NAME = "linux-dotnet.yml"
_COMPOSE_FILE_PATH = (C.PROJ_ROOT / "utils/docker-compose" / _COMPOSE_FILE_NAME).resolve()
assert _COMPOSE_FILE_PATH.is_file()


def main() -> int:

    parser = C.get_docker_parser()
    parser.add_argument(
        "db_password",
    )
    args = parser.parse_args()

    docker = "podman" if args.podman else "docker"

    C.start_containers(
        _COMPOSE_FILE_PATH,
        docker=docker,
        env={
            "DB_PASSWORD": args.db_password,
        },
        checks=(
            "rabbitmq rabbitmq-diagnostics -q ping",
            f"mariadb mysqladmin -uroot -p{args.db_password} status",
            "redis /usr/local/bin/redis-cli PING",
            f"mssql /opt/mssql-tools/bin/sqlcmd -U sa -P {args.db_password} -l 1 -Q 'SELECT @@VERSION'",
            "mongodb /usr/bin/mongo --quiet --eval 'db.stats().ok'",
        ),
    )

    # Start MongoDB replication
    cmd = (
        docker, "exec",
        "mongodb",
        "/usr/bin/mongo", "--eval", "rs.initiate()",
    )
    C.run(cmd)

    return 0


if __name__ == "__main__":
    raise SystemExit(main())
