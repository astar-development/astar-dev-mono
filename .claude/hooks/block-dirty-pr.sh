#!/usr/bin/env bash
command=$(jq -r '.tool_input.command // ""')
case "$command" in
  *"gh pr create"*|*"gh pr merge"*"--auto"*) ;;
  *) exit 0 ;;
esac
if [ -n "$(git status --porcelain --untracked-files=no -- . ':!.serena')" ]; then
  echo "Uncommitted changes present (git status). Commit related changes before the PR / auto-merge." >&2
  exit 2
fi
if git diff main...HEAD --summary | grep -q 'mode change'; then
  echo "Branch contains file-mode-only changes (git diff main...HEAD --summary). Remove them." >&2
  exit 2
fi
exit 0
