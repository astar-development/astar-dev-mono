---
name: fix-issue
description: Fix a GitHub issue end-to-end with TDD, branch hygiene, and verified tests
disable-model-invocation: true
---

Fix GitHub issue identified by the numeric issue ID passed as $ARGUMENTS (for example, 123). If the input is a URL, first extract the issue number before running any commands.

**BEFORE ANY CODE:**

1. Run `gh repo view --json nameWithOwner -q '.nameWithOwner'` — confirm the repository is correct. Confirm that the issue affects exactly one top-level project under `src/<project-name>/` and list the affected path(s).
    - If the repo is incorrect, the issue scope does not match any `src/` folder, or the branch cannot be created, stop immediately and report the failure before making changes.
    - If `gh repo view`, `gh issue view`, `git branch`, `git checkout -b`, or Serena initialization fails, stop, paste the command output, and ask for guidance before continuing.
2. Run `git branch` — confirm NOT on `main`. Create the branch as `git checkout -b fix/<issue-number>-<lowercase-kebab-case-summary>` using the numeric issue ID from $ARGUMENTS and a summary of 2-4 words with no spaces.
3. Call Serena `initial_instructions` before exploring the codebase.
4. Use `gh issue view $ARGUMENTS` to get issue details.

**IMPLEMENT (strict TDD):** 5. Write a failing RED test first — commit it before writing production code. If you cannot create a failing test that reproduces the issue, stop and ask for clarification instead of inventing a test or changing production code. 6. Use `mcp__serena__find_symbol` / `mcp__serena__find_referencing_symbols` for symbol lookups — do NOT read whole files for exploration. Search first; if you cannot identify all call sites within 5 file reads, stop and report the remaining unknowns instead of guessing. 7. Find all call sites and test files before touching production code. Do not claim to have found all call sites unless the search results prove it. 10. Implement minimal production code to make the RED test pass. 11. Use idiomatic `Match`/`MatchAsync` — never tuple-intermediate patterns. 12. Use primary constructors where applicable per style guide. 13. Reuse existing `LogMessage` templates — do NOT create new ones unless no suitable template exists.

**VERIFY (mandatory — no exceptions):** 14. Run `dotnet build` — must be zero errors, zero warnings. Paste exact output. 15. Run `dotnet test` — paste the EXACT pass/fail count from raw terminal output. Do NOT summarise or self-report. Do NOT claim passing without showing the output. 16. If new failures appear, diagnose and fix them. NEVER dismiss failures as pre-existing — if this change broke them, own it and fix it.

**COMPLETE:** 17. Stop and request human review before committing. 18. After approval: commit to the feature branch, then `gh pr create`.
