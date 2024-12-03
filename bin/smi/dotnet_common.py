"""Common variables and functions for smi"""

import argparse


def add_args(
    parser: argparse.ArgumentParser,
    configuration: str = "debug",
) -> None:
    parser.add_argument(
        "-c",
        "--configuration",
        type=str.title,
        choices=("Debug", "Release"),
        default=configuration,
    )
