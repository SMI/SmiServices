#!/usr/bin/env python3
import os
import platform
import sys

sys.path.append(os.path.join(os.path.dirname(__file__), ".."))
import common  # noqa: E402

_PLAT = "-arm" if platform.processor() == "arm" else ""
_COMPOSE_FILE_NAME = f"linux-dotnet{_PLAT}.yml"
_COMPOSE_FILE_PATH = f"{common.PROJ_ROOT}/utils/docker-compose/{_COMPOSE_FILE_NAME}"
assert os.path.isfile(
    _COMPOSE_FILE_PATH,
), f"Compose file does not exist: {_COMPOSE_FILE_PATH}"


def main() -> int:

    parser = common.get_docker_parser()
    parser.add_argument(
        "db_password",
    )
    args = parser.parse_args()

    docker = "podman" if args.podman else "docker"

    common.start_containers(
        _COMPOSE_FILE_PATH,
        docker=docker,
        env={
            "DB_PASSWORD": args.db_password,
        },
        checks=(
            "rabbitmq rabbitmq-diagnostics -q ping",
            f"mariadb mysqladmin -uroot -p{args.db_password} status",
            "redis /usr/local/bin/redis-cli PING",
            f"mssql /opt/mssql-tools18/bin/sqlcmd -U sa -P {args.db_password} -No -l 1 -Q 'SELECT @@VERSION'",
            "mongodb /usr/bin/mongo --quiet --eval 'db.stats().ok'",
        ),
    )

    # Start MongoDB replication
    cmd = (
        docker,
        "exec",
        "mongodb",
        "/usr/bin/mongo",
        "--eval",
        'rs.initiate({_id:"rs0",members:[{_id:0,host:"localhost:27017"}]})',
    )
    common.run(cmd)

    return 0


if __name__ == "__main__":
    raise SystemExit(main())
