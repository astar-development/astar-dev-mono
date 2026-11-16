---
name: sonar-fix
description: Check the current PR's SonarCloud result and fix new issues and coverage gaps. Use for "sonar fix", "fix sonar", after a PR is opened.
---

1. `gh pr checks` then read the SonarCloud comment on the PR (`gh pr view --comments`).
2. List new issues: rule id, file, line, and new-code coverage and duplication.
3. Fix each: S107 -> parameter object; S3358 -> if/else or switch expression; coverage < 80% -> boundary tests for uncovered new branches; duplication > 3% -> extract shared code.
4. Build and test only affected projects, commit, `git fetch origin main && git merge origin/main`, push.
5. Re-check the Sonar gate. Report gate status, not just issue count.
