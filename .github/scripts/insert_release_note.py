#!/usr/bin/env python3
"""Prepend a version entry into a .csproj's <PackageReleaseNotes> block.

Used by release-notes-bump.yml (pre-merge) — every packable project's
PackageReleaseNotes is normalized to the same multi-line block shape, so
this only ever needs to insert right after the opening tag.

Idempotent: if the desired entry is already the first one in the block,
nothing is written. release-notes-bump.yml runs on every PR push, including
the one it creates itself pushing this file's own edit — without this check
that would re-insert a duplicate entry on every subsequent run.

Prints "changed" or "unchanged" to stdout so the caller knows whether to
stage the file.
"""

import sys


def main() -> None:
    path, version, title = sys.argv[1], sys.argv[2], sys.argv[3]

    with open(path, encoding="utf-8") as handle:
        text = handle.read()

    marker = "<PackageReleaseNotes>"
    insert_at = text.index(marker) + len(marker)
    desired_entry = f"v{version} {title}"

    line_start = insert_at + 1  # skip the newline right after the opening tag
    line_end = text.index("\n", line_start)
    current_first_entry = text[line_start:line_end].strip()

    if current_first_entry == desired_entry:
        print("unchanged")
        return

    entry = f"\n        {desired_entry}"
    text = text[:insert_at] + entry + text[insert_at:]

    with open(path, "w", encoding="utf-8") as handle:
        handle.write(text)

    print("changed")


if __name__ == "__main__":
    main()
