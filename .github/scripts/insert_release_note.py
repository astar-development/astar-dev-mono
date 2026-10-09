#!/usr/bin/env python3
"""Prepend a version entry into a .csproj's <PackageReleaseNotes> block.

Used by release-notes-bump.yml (pre-merge) — every packable project's
PackageReleaseNotes is normalized to the same multi-line block shape, so
this inserts right after the opening tag, normalizes an inline block to the multi-line shape, and adds a block to projects that have none.

Idempotent: if the desired entry is already the first one in the block,
nothing is written. release-notes-bump.yml runs on every PR push, including
the one it creates itself pushing this file's own edit — without this check
that would re-insert a duplicate entry on every subsequent run.

Prints "changed" or "unchanged" to stdout so the caller knows whether to
stage the file.
"""

import re
import sys
from xml.sax.saxutils import escape


def main() -> None:
    path, version, title = sys.argv[1], sys.argv[2], sys.argv[3]

    with open(path, encoding="utf-8") as handle:
        text = handle.read()

    desired_entry = escape(f"v{version} {title}")
    block = re.search(r"<PackageReleaseNotes>(.*?)</PackageReleaseNotes>", text, re.DOTALL)

    if block is None:
        property_group = re.search(r"<PropertyGroup>[ \t]*\r?\n", text)
        if property_group is None:
            print("unchanged")
            return

        insert_at = property_group.end()
        new_block = f"        <PackageReleaseNotes>\n        {desired_entry}\n        </PackageReleaseNotes>\n"
        text = text[:insert_at] + new_block + text[insert_at:]
    else:
        body = block.group(1)
        first_entry = next((line.strip() for line in body.splitlines() if line.strip()), "")

        if first_entry == desired_entry:
            print("unchanged")
            return

        if body.startswith("\n") or body.startswith("\r\n"):
            replacement = f"\n        {desired_entry}{body}"
        else:
            replacement = f"\n        {desired_entry}\n        {body.strip()}\n        "

        text = text[: block.start(1)] + replacement + text[block.end(1):]

    with open(path, "w", encoding="utf-8") as handle:
        handle.write(text)

    print("changed")


if __name__ == "__main__":
    main()
