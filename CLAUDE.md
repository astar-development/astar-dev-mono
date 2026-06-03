# CLAUDE.md

## Repo

Mono-repo: Blazor web, Avalonia desktop, ~25 NuGet packages. Solution: `AStar.Dev.slnx`.

## Build Gotcha

```bash
# Stale generated files / changed base props
dotnet clean && rm -rf artifacts/ && dotnet build
```

## Mandatory NuGet Packages

Check before writing new code — use when practicable:

- `AStar.Dev.Functional.Extensions` — `Result<T>`, `Option<T>`, `Map`/`Bind`/`MatchAsync`
- `AStar.Dev.Logging.Extensions` — compile-time `LogMessage` templates; avoid `logger.Log...`
- `AStar.Dev.Utilities` — string extensions, nullability, `CombinePaths` etc.

New reusable code → add to relevant package + raise GitHub issue.

## Logging

ALL logging → Azure Application Insights. No suitable `LogMessage` template? ADD IT.
Reuse existing `LogMessage` templates — do NOT create new ones unless no suitable template exists.

## DI

DI from the start. Never `new` a service inside a class.

## Architecture

- `Directory.Build.props/targets` and `Directory.Packages.props` centralized — never duplicate in `.csproj`.
- Child `Directory.Build.props` MUST import parent via `$([MSBuild]::GetPathOfFileAbove(...))`.
- All `bin/` and `obj/` redirect to `artifacts/`.
- Local dev: `<ProjectReference>` not `<PackageReference>` (avoids publish cycles).
- CI version: `-p:Version=$(GitTag)`; local fallback `0.1.0`. Tag format: `v1.2.3`.

## Conventions

- **Commits**: Conventional Commits — `feat(scope): ...`, `fix(scope): ...`
- **Branches**: `feature/...`, `bug/...`, `fix/...`, `doc/...`; `main` always deployable
- **Method signatures**: single-line regardless of param count. Split ONLY at >200 chars.
- **Comments**: only when _reason_ isn't derivable from code. Never restate what code does.
- **XML comments**: all public members. Implementing interface → `<inheritdoc />` only.
- **Test projects**: `*.Tests.Unit` / `*.Tests.Integration`
- **Blank line before `return`** after a code block. NOT after `if`/`else`.

Patterns: see @.claude/rules/c-sharp-code-style.md and @.claude/rules/avalonia-ui.md.

## Before Starting ANY Task (mandatory, no exceptions)

1. **Repo + folder** — run `gh repo view --json nameWithOwner -q '.nameWithOwner'` and confirm the correct `src/` folder for the issue scope.
2. **Branch** — confirm not on `main`. Create branch first.
3. **TDD** — commit failing test BEFORE writing production code.
4. **Scope** — implement only what was asked. Stop for review before touching other areas.

## Code Exploration

- Call Serena `initial_instructions` BEFORE exploring — no exceptions.
- Use `mcp__serena__find_symbol` / `mcp__serena__find_referencing_symbols` — do NOT read whole files.
- Cap at 5 file reads before stating a plan. Do not keep reading without producing a fix.
- Find ALL call sites and test files before touching production code.
- Read a file before editing it. Grep all callers before modifying a function.

## Definition of Done

1. `dotnet build` — zero errors, zero warnings. Paste exact output.
2. `dotnet test` — paste EXACT pass/fail count. New failures = zero. If this change broke tests, diagnose and fix them — never dismiss as pre-existing.
3. Confirm all call sites and test files found and updated.
4. Human review BEFORE committing.
5. Approved → commit to branch, raise GitHub PR.

Never say "fixed"/"done" without evidence. Say "I believe this is fixed because…"
For sync/download bugs: confirm full flow (Graph API → persistence → sync logic) first.

## GitHub

Always use `gh` CLI for all GitHub operations. Never use MCP GitHub - not configured.
When raising a PR, use `.github/PULL_REQUEST_TEMPLATE.md` as the body structure — fill in each section; do not omit or rewrite the template.

## Subagents

- `c-sharp-qa` → tests; `c-sharp-dev` → C# features; `c-sharp-reviewer` → code review.
- After any subagent: `Read` every claimed file, re-run `dotnet test` yourself, verify diff.
- Subagent drifts → take over directly, don't re-prompt.

## graphify

Knowledge graph at `graphify-out/`. Before architecture questions, read `graphify-out/GRAPH_REPORT.md`.
Use `graphify query/path/explain` for cross-module questions. After modifying code, run `graphify update .`.
