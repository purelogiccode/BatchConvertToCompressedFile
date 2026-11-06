#!/usr/bin/env python3
"""Create a .tar.gz bundle with correct Unix modes on any host OS."""

from __future__ import annotations

import os
import sys
import tarfile
from pathlib import Path


def main() -> None:
    if len(sys.argv) != 3:
        raise SystemExit("usage: package-tar.py <source-directory> <output.tar.gz>")

    source = Path(sys.argv[1])
    target = Path(sys.argv[2])
    executable_name = "BatchConvertToCompressedFile"

    with tarfile.open(target, "w:gz") as archive:
        root_info = archive.gettarinfo(str(source), arcname=".")
        root_info.mode = 0o755
        archive.addfile(root_info)

        for root, directories, files in os.walk(source):
            root_path = Path(root)
            for directory in sorted(directories):
                path = root_path / directory
                info = archive.gettarinfo(str(path), arcname=str(path.relative_to(source)))
                info.mode = 0o755
                archive.addfile(info)
            for file_name in sorted(files):
                path = root_path / file_name
                info = archive.gettarinfo(str(path), arcname=str(path.relative_to(source)))
                info.mode = 0o755 if file_name == executable_name else 0o644
                with path.open("rb") as stream:
                    archive.addfile(info, stream)


if __name__ == "__main__":
    main()
