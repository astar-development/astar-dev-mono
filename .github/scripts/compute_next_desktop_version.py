#!/usr/bin/env python3
"""Print the next semantic version for a desktop app.

Usage: compute_next_desktop_version.py <tag_prefix> <tags_file> <messages_file>

<tags_file> holds the app's existing tags, one per line. <messages_file>
holds the commit messages being released, NUL separated (as produced by
`git log --format=%B%x00`). Only stable tags (<tag_prefix>X.Y.Z) count.
The bump follows Conventional Commits, highest wins: a '!' in the subject
or a BREAKING CHANGE footer is major, feat is minor, anything else is
patch. An app with no stable tag yet starts at 0.1.0.
"""

import pathlib
import re
import sys

BUMP_ORDER = {"patch": 0, "minor": 1, "major": 2}
FIRST_VERSION = "0.1.0"
SUBJECT_PATTERN = re.compile(r"^(?P<type>\w+)(\([^)]*\))?(?P<bang>!)?:")
BREAKING_FOOTER_PATTERN = re.compile(r"^BREAKING[ -]CHANGE:", re.MULTILINE)


def split_messages(text: str) -> list[str]:
    return [message.strip() for message in text.split("\0") if message.strip()]


def message_bump(message: str) -> str:
    subject = SUBJECT_PATTERN.match((message.splitlines() or [""])[0])
    if BREAKING_FOOTER_PATTERN.search(message) or (subject and subject.group("bang")):
        return "major"

    return "minor" if subject and subject.group("type") == "feat" else "patch"


def bump_kind(messages: list[str]) -> str:
    return max((message_bump(message) for message in messages), key=BUMP_ORDER.__getitem__, default="patch")


def latest_stable_version(tags: list[str], tag_prefix: str) -> tuple[int, int, int] | None:
    pattern = re.compile(re.escape(tag_prefix) + r"(\d+)\.(\d+)\.(\d+)$")
    versions = [tuple(int(part) for part in match.groups()) for match in map(pattern.match, tags) if match]

    return max(versions, default=None)


def compute_next_version(tags: list[str], tag_prefix: str, messages: list[str]) -> str:
    latest = latest_stable_version(tags, tag_prefix)
    if latest is None:
        return FIRST_VERSION

    major, minor, patch = latest
    bump = bump_kind(messages)
    if bump == "major":
        return f"{major + 1}.0.0"

    return f"{major}.{minor + 1}.0" if bump == "minor" else f"{major}.{minor}.{patch + 1}"


def main() -> None:
    tag_prefix, tags_file, messages_file = sys.argv[1:4]
    tags = pathlib.Path(tags_file).read_text(encoding="utf-8").split()
    messages = split_messages(pathlib.Path(messages_file).read_text(encoding="utf-8"))

    print(compute_next_version(tags, tag_prefix, messages))


if __name__ == "__main__":
    main()
