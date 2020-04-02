"""
Parses the given RabbitMQ JSON config and prints it in a human-readable format
"""
# TODO(rkm 2020-01-30) Hook this up to the CI build
# TODO(rkm 2020-01-30) Handle multiple vhosts
# TODO(rkm 2020-01-30) Print other exchange info - type, durability etc.
# TODO(rkm 2020-01-30) Print policy info

import argparse
import json
import sys
from pathlib import Path
from typing import List


def main() -> int:

    parser = argparse.ArgumentParser()
    parser.add_argument("file")
    args = parser.parse_args()

    config_file = Path(args.file)
    print(f"RabbitMQ config from {config_file.resolve()}")
    if not config_file.exists():
        print(f"Could not find input file '{config_file}'", file=sys.stderr)
        return 1
    with open(config_file) as f:
        data = json.load(f)

    vhosts = data["vhosts"]
    if len(vhosts) > 1:
        print(
            "Config defnies multiple vhosts - currently unspported by this script",
            file=sys.stderr,
        )
        return 1
    for vhost in vhosts:
        print(f"Virtual Host: {vhost['name']}")

    def any_duplicates(l: List) -> bool:
        dups = set([x for x in l if l.count(x) > 1])
        if dups:
            print(f"Duplicate enttiy name in {dups.sort()}", file=sys.stderr)
            return True
        return False

    exchange_names = [x["name"] for x in data["exchanges"]]
    if any_duplicates(exchange_names):
        return 1

    queue_names = [x["name"] for x in data["queues"]]
    if any_duplicates(queue_names):
        return 1

    def print_binding(exchange: str, queue: str, routing_key: str) -> None:
        routing_key_str = f'with key "{routing_key}"' if routing_key else ""
        print(f"    {exchange} -> {queue} {routing_key_str}")

    used_exchanges = set()
    used_queues = set()

    print("Bindings:")
    for binding in sorted(data["bindings"], key=lambda x: x["source"]):
        source_exchange = binding["source"]
        if source_exchange not in exchange_names:
            print(
                f'Undefined exchange "{source_exchange}" for binding', file=sys.stderr
            )
            return 1
        # NOTE(rkm 2020-01-30) Exchanges can technically be bound to other exchanges
        destination_queue = binding["destination"]
        if destination_queue not in queue_names:
            print(f'Undefined queue "{source_exchange}" for binding', file=sys.stderr)
            return 1
        used_exchanges.add(source_exchange)
        used_queues.add(destination_queue)
        print_binding(source_exchange, destination_queue, binding["routing_key"])

    # Unbound exchanges may be intentional
    unused = set(exchange_names) - used_exchanges
    for exchange in unused:
        print(f"Warning: Unbound exchange {exchange}", file=sys.stderr)

    unused = set(queue_names) - used_queues
    if unused:
        for queue in unused:
            print(f"Unbound queue {queue}", file=sys.stderr)
        return 1

    return 0


if __name__ == "__main__":
    # NOTE(rkm 2020-01-30) Return code not always obvious on Windows
    rc = main()
    print(f"Errors: {rc != 0}")
    exit(rc)
