#!/usr/bin/env python3

import argparse
import shutil
import os
import sys
import tempfile
from pathlib import Path

sys.path.append(os.path.join(os.path.dirname(__file__), '..'))
import common as C

import pythonCommon as PC
import test as T


def main() -> int:

    parser = argparse.ArgumentParser()
    C.add_clean_arg(parser)
    C.add_tag_arg(parser)
    parser.add_argument(
        "python_build_exe",
        type=Path,
        help="Path to the python exe to use for the build, relative to the project root",
    )
    args = parser.parse_args()

    dist_tag_dir = C.DIST_DIR / args.tag

    if args.clean:
        shutil.rmtree(dist_tag_dir)
    dist_tag_dir.mkdir(parents=True, exist_ok=True)

    python_exe = Path(os.path.abspath(C.PROJ_ROOT / args.python_build_exe))
    if os.name == "nt":
        python_exe = python_exe.with_suffix(".exe")
    if not python_exe.is_file():
        print(f"Error: {python_exe} not found", file=sys.stderr)
        return 1

    cmd = (
        python_exe,
        "-m", "pip",
        "install",
        "-r", (PC.PY_DIR / "requirements.txt").resolve(),
    )
    C.run(cmd)

    rc = T.main(("-p", str(python_exe)))
    if rc:
        return rc

    cmd = (
        python_exe,
        (PC.PY_DIR / "setup.py").resolve(),
        "bdist_wheel",
        "-d", dist_tag_dir.resolve(),
    )

    with tempfile.TemporaryDirectory() as tmpdir:
        C.run(cmd, cwd=tmpdir)

    C.create_checksums(dist_tag_dir, "python")

    return 0


if __name__ == "__main__":
    raise SystemExit(main())
