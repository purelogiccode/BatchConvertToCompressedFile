#!/usr/bin/env python3
"""Build the GitHub Wiki payload from the MkDocs documentation pages."""

from __future__ import annotations

import re
import shutil
from pathlib import Path

ROOT = Path(__file__).resolve().parent.parent
SOURCE = ROOT / "docs"
TARGET = ROOT / "wiki-build"

LINK_PATTERN = re.compile(r"\]\(([^)\s#]+)\.md(#[^)]*)?\)")


def rewrite_links(text: str) -> str:
    def replace(match: re.Match[str]) -> str:
        target = match.group(1)
        if target.startswith(("http://", "https://", "mailto:")):
            return match.group(0)
        if target in {"index", "./index"}:
            target = "Home"
        return f"]({target}{match.group(2) or ''})"

    return LINK_PATTERN.sub(replace, text)


def main() -> None:
    if TARGET.exists():
        shutil.rmtree(TARGET)
    TARGET.mkdir(parents=True)

    for source in sorted(SOURCE.glob("*.md")):
        target_name = "Home.md" if source.name == "index.md" else source.name
        text = source.read_text(encoding="utf-8")
        (TARGET / target_name).write_text(rewrite_links(text), encoding="utf-8")

    print(f"Built {len(list(TARGET.glob('*.md')))} wiki pages in {TARGET}")


if __name__ == "__main__":
    main()
