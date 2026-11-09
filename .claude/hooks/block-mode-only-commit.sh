#!/usr/bin/env bash
command=$(jq -r '.tool_input.command // ""')
case "$command" in
  *"git commit"*) ;;
  *) exit 0 ;;
esac
if git diff --cached --summary | grep -q 'mode change'; then
  echo "Staged file-mode change detected (git diff --cached --summary). Unstage it before committing." >&2
  exit 2
fi
exit 0
