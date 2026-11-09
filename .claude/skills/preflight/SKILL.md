---
name: preflight
description: Verify a branch is merge-ready before opening a PR - committed changes, targeted build, affected tests with coverage, Sonar-style scan. Use for "preflight", "is this ready for a PR", before issue-to-PR hands off.
---

1. `git status`: every related change (including wiki/, KB, docs) is committed. No file-mode-only changes in `git diff --summary`.
2. Build only the projects touched by `git diff --name-only main...HEAD`: `dotnet clean <proj> && dotnet build <proj>`. Never clean-build the whole mono-repo. Zero errors, zero warnings.
3. Run `dotnet test --no-build` on affected test projects with coverage. New-code coverage must be >= 80%. Add boundary tests for every uncovered new branch.
4. Scan the diff for Sonar rules: methods or constructors with more than 7 parameters (S107, use a parameter object), nested ternaries (S3358, use if/else or a switch expression).
5. Report a pass/fail checklist with exact build output and test counts. On any failure, fix on the same branch and rerun. Do not enable auto-merge until all pass.
