---
name: fix-issue
description: Fix a GitHub issue end-to-end with TDD, branch hygiene, and verified tests
disable-model-invocation: true
---
Fix GitHub issue: $ARGUMENTS.

**BEFORE ANY CODE:**
1. Run `git branch` — confirm NOT on `main`. Create branch: `git checkout -b fix/<issue-number>-short-description`
2. Call Serena `initial_instructions` before exploring the codebase
3. Use `gh issue view $ARGUMENTS` to get issue details

**IMPLEMENT (strict TDD):**
4. Write a failing RED test first — commit it before writing production code
5. Use `mcp__serena__find_symbol` / `mcp__serena__find_referencing_symbols` for symbol lookups — do NOT read whole files for exploration
6. Find ALL call sites and test files before touching production code
7. Implement minimal production code to make the RED test pass
8. Use idiomatic `Match`/`MatchAsync` — never tuple-intermediate patterns
9. Use primary constructors where applicable per style guide

**VERIFY (mandatory — no exceptions):**
10. Run `dotnet build` — must be zero errors, zero warnings. Paste exact output.
11. Run `dotnet test` — paste the EXACT pass/fail count from raw terminal output. Do NOT summarise or self-report. Do NOT claim passing without showing the output.
12. Confirm new failures = 0 (pre-existing failures are acceptable but must be identified)

**COMPLETE:**
13. Stop and request human review before committing
14. After approval: commit to the feature branch, then `gh pr create`
