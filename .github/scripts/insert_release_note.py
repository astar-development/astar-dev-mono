#!/usr/bin/env python3
"""Prepend a version entry into a .csproj's <PackageReleaseNotes> block.

Used by release-notes-bump.yml (pre-merge) — every packable project's
PackageReleaseNotes is normalized to the same multi-line block shape, so
this inserts right after the opening tag, normalizes an inline block to the multi-line shape, and adds a block to projects that have none.

Idempotent: any existing entry for the same version is replaced, so each
version appears once even when the PR title is edited between runs, and if
the result matches the file nothing is written. release-notes-bump.yml runs
on every PR push, including the one it creates itself pushing this file's
own edit — without this check that would re-insert a duplicate entry on
every subsequent run.

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
        same_version = re.escape(f"v{version}") + r"(?![^\s])"
        body = re.sub(r"\r?\n[ \t]*" + same_version + r"[^\r\n]*(?=\r?\n)", "", block.group(1))

        if body.startswith("\n") or body.startswith("\r\n"):
            replacement = f"\n        {desired_entry}{body}"
        elif body.strip() == "" or re.match(same_version, body.strip()):
            replacement = f"\n        {desired_entry}\n        "
        else:
            replacement = f"\n        {desired_entry}\n        {body.strip()}\n        "

        new_text = text[: block.start(1)] + replacement + text[block.end(1):]
        if new_text == text:
            print("unchanged")
            return

        text = new_text

    with open(path, "w", encoding="utf-8") as handle:
        handle.write(text)

    print("changed")


if __name__ == "__main__":
    main()
