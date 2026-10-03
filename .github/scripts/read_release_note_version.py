#!/usr/bin/env python3
"""Print the version from a csproj's newest PackageReleaseNotes entry.

Used by release-on-merge.yml. By the time a merge lands on main, the
release notes have already been bumped pre-merge (see
release-notes-bump.yml) — this only ever reads, never writes, and just
tells the caller which version to tag.

Prints nothing (and exits 0) if the project has no PackageReleaseNotes
block at all, so the caller can skip tagging it.
"""

import re
import sys


def main() -> None:
    path = sys.argv[1]

    with open(path, encoding="utf-8") as handle:
        text = handle.read()

    marker = "<PackageReleaseNotes>"
    if marker not in text:
        return

    rest = text[text.index(marker) + len(marker):]
    match = re.search(r"v(\d+\.\d+\.\d+(?:-[\w.]+)?)", rest)
    if match:
        print(match.group(1))


if __name__ == "__main__":
    main()
