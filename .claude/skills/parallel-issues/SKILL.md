---
name: parallel-issues
description: Work several independent GitHub issues in parallel, one subagent and worktree each, following issue-to-pr. Use for "do these issues in parallel", "clear the backlog".
---

1. `gh issue list` - group issues by disjoint files/projects. Dependent issues wait for their dependency to merge.
2. Per independent issue: subagent in own worktree (`git worktree add ../wt-<issue>`), following the `issue-to-pr` skill (red tests, green, scoped build, PR, auto-merge). Absolute paths only.
3. Coordinate: poll `gh pr checks`; send CI/Sonar failures back to the owning subagent.
4. After each subagent: read claimed files, re-run its tests, verify diff.
5. End: table issue -> PR -> status -> remaining Sonar warnings. Remove worktrees.
