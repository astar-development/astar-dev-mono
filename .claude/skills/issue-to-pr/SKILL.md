---
name: issue-to-pr
description: Run the full astar-dev-mono delivery loop for one change - GitHub issue, branch, TDD (red then green), clean build, scoped tests, PR with auto-merge, confirm the issue closed, capture learnings in the KB. Use for "issue to PR", "deliver issue 123", "do the whole loop for this bug".
argument-hint: <issue number, or a short description of the change>
---

Follow the repo `CLAUDE.md` rules. This skill fixes the order; it does not relax any rule. Stop and ask when a step cannot be completed. Never skip a step silently.

1. **Repo and scope.** Run `gh repo view --json nameWithOwner -q '.nameWithOwner'`. Confirm the correct `src/` or `apps/` folder for the issue.
2. **Issue.** Argument is a number: `gh issue view <n>`. Argument is a description: search `gh issue list --search` first, then `gh issue create` if none fits. One issue per distinct bug or phase.
3. **Branch.** Confirm not on `main`. Create `feature/<n>-short-description` or `bug/<n>-short-description` from up-to-date `main`.
4. **Explore.** Call Serena `initial_instructions`, then use `find_symbol` / `find_referencing_symbols`. Find all call sites and test files before touching production code. Read the KB pages named in `CLAUDE.local.md` for the topic.
5. **Red.** Write the failing test. Run only that test project or class. Confirm it fails for the right reason. Commit the test alone: `test(scope): ... (#n)`.
6. **Green.** Implement the minimum change. Commit production code separately: `feat(scope): ...` or `fix(scope): ...`. Touch nothing unrelated; suggest other improvements as separate items.
7. **Verify.** `dotnet clean && dotnet build --no-restore` must give zero errors and zero warnings. Then run only the affected and new tests with `dotnet test --no-build`. Report the exact pass/fail counts. Never run the full suite; the `SessionEnd` hook does that.
8. **Pull request.** Push the branch. Raise the PR with `.github/PULL_REQUEST_TEMPLATE.md`, all sections filled, body containing `Fixes #<n>`. Run `gh pr merge <pr> --auto --squash`. Never request a reviewer.
9. **Confirm closed.** Check `gh pr view <pr> --json state,mergedAt,statusCheckRollup` and `gh issue view <n> --json state`. Report one of: merged and issue closed, auto-merge pending (name the blocking check or review), or checks failed (name the failure and fix it on the branch). Do not say "done" or "fixed" without this evidence.
10. **Knowledge base.** If `/mnt/tbdrive/AI-KnowledgeBase/` exists, capture gotchas, decisions with reasons and non-obvious findings per its `CLAUDE.md` Auto-capture rules. Update an existing page rather than adding a duplicate. Skip silently when the drive is missing.
11. **Final report.** List every issue and PR raised or touched with status (merged, open, uninvestigated) and the next step, plus the KB pages captured.
