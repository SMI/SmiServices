#!/usr/bin/env python3

import argparse
import os
import sys
from pathlib import Path
from typing import Optional
from typing import Sequence

sys.path.append(os.path.join(os.path.dirname(__file__), '..'))
import common as C

import pythonCommon as PC


def main(argv: Optional[Sequence[str]] = None) -> int:

    parser = argparse.ArgumentParser()
    parser.add_argument(
        "-p",
        "--python_exe",
        type=Path,
        help="Path to the python exe to use for running tests, relative to the project root",
    )
    args = parser.parse_args(argv)

    python_exe = "python"
    if args.python_exe:
        python_exe = Path(os.path.abspath(C.PROJ_ROOT / args.python_exe))
        if not python_exe.is_file():
            print(f"Error: {python_exe} not found", file=sys.stderr)
            return 1

    cmd = (
        python_exe,
        "-m", "pip",
        "install",
        "-r", (PC.PY_DIR / "requirements-dev.txt").resolve(),
    )
    C.run(cmd)

    smiservices_py_dir = PC.PY_DIR / "SmiServices"
    tests = (x.name for x in smiservices_py_dir.glob("*.py"))
    cmd = (
        python_exe,
        "-m", "pytest",
        *tests
    )
    C.run(cmd, cwd=smiservices_py_dir)

    return 0


if __name__ == "__main__":
    raise SystemExit(main())
