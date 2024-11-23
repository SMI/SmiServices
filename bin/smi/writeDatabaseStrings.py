#!/usr/bin/env python3
import argparse
import os
import sys

sys.path.append(os.path.join(os.path.dirname(__file__), ".."))
import common  # noqa: E402

_RELATIONAL_YAML = (
    f"{common.PROJ_ROOT}/tests/SmiServices.UnitTests/RelationalDatabases.yaml"
)
assert os.path.isfile(_RELATIONAL_YAML), f"YAML file does not exist: {_RELATIONAL_YAML}"

_TEST_DBS_TXT = f"{common.PROJ_ROOT}/tests/SmiServices.UnitTests/TestDatabases.txt"
assert os.path.isfile(
    _TEST_DBS_TXT,
), f"Test databases file does not exist: {_TEST_DBS_TXT}"


def main() -> int:

    parser = argparse.ArgumentParser()
    parser.add_argument(
        "mssql_server",
    )
    parser.add_argument(
        "db_password",
    )
    args = parser.parse_args()

    with open(_RELATIONAL_YAML, "w") as f:
        # NOTE(rkm 2022-02-27) localdb installs with permissions for current user, so
        # don't set user or password
        if "localdb" in args.mssql_server:
            f.write(f"SqlServer: 'Server={args.mssql_server};'\n")
        else:
            f.write(
                f"SqlServer: 'Server={args.mssql_server};User Id=sa;"
                f"Password={args.db_password};TrustServerCertificate=true;'\n",
            )

        # NOTE(rkm 2022-02-27) We don't run MySQL in Windows in GitHub actions
        if not (os.name == "nt" and common.is_ci()):
            f.write(
                f"MySql: 'server=127.0.0.1;Uid=root;Pwd={args.db_password};sslmode=None'\n",
            )

    with open(_RELATIONAL_YAML) as f:
        print(f"{_RELATIONAL_YAML}:")
        print(f.read())

    with open(_TEST_DBS_TXT, "w") as f:
        f.write(f"ServerName: {args.mssql_server}\n")
        f.write("Prefix: TEST_\n")
        f.write("Username: sa\n")
        f.write(f"Password: {args.db_password}\n")
        if not (os.name == "nt" and common.is_ci()):
            f.write(
                f"MySql: server=127.0.0.1;Uid=root;Pwd={args.db_password};sslmode=None\n",
            )

    with open(_TEST_DBS_TXT) as f:
        print(f"{_TEST_DBS_TXT}:")
        print(f.read())

    return 0


if __name__ == "__main__":
    raise SystemExit(main())
