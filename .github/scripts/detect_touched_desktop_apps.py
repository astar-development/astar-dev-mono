#!/usr/bin/env python3
"""Print the desktop apps touched by a set of changed files, as a JSON array.

Reads newline separated changed file paths on stdin (e.g. the output of
`git diff --name-only`) and the app definitions from desktop-apps.json,
then prints the definitions of every app with at least one changed file
under one of its directories. Apps keep their order from the config file.
Printing `[]` means no desktop app was touched.
"""

import json
import pathlib
import sys

DEFAULT_CONFIG = pathlib.Path(__file__).resolve().parent.parent / "desktop-apps.json"


def parse_changed_files(text: str) -> list[str]:
    return [line.strip() for line in text.splitlines() if line.strip()]


def detect_touched_apps(changed_files: list[str], apps: list[dict]) -> list[dict]:
    return [app for app in apps if any(path.startswith(tuple(app["directories"])) for path in changed_files)]


def main() -> None:
    config_path = pathlib.Path(sys.argv[1]) if len(sys.argv) > 1 else DEFAULT_CONFIG
    apps = json.loads(config_path.read_text(encoding="utf-8"))
    touched = detect_touched_apps(parse_changed_files(sys.stdin.read()), apps)

    print(json.dumps(touched, separators=(",", ":")))


if __name__ == "__main__":
    main()
