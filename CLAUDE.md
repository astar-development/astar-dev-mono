# CLAUDE.md

## Repo

Mono-repo: Blazor web, Avalonia desktop, ~25 NuGet packages. Solution: `AStar.Dev.slnx`.

## Build — mandatory clean before every verification

`TreatWarningsAsErrors=true` set unconditionally in `Directory.Build.props`. Incremental builds cache stale analyzer results, hide warnings. **Clean first always** — no exceptions:

```bash
dotnet clean && dotnet build --no-restore
```

Never use `dotnet build` alone as done check. Artifacts suspect → wipe full:

```bash
dotnet clean && rm -rf artifacts/ && dotnet build
```

## Mandatory NuGet Packages

Check before writing new code — use when practicable:

- `AStar.Dev.Functional.Extensions` — `Result<T>`, `Option<T>`, `Map`/`Bind`/`MatchAsync`
- `AStar.Dev.Logging.Extensions` — compile-time `LogMessage` templates; avoid `logger.Log...`
- `AStarDev.Utilities` — string extensions, nullability, `CombinePaths` etc.

New reusable code → add to relevant package + raise GitHub issue.

## Logging

ALL logging → Azure Application Insights. No fit `LogMessage` template? ADD IT.
Reuse existing `LogMessage` templates — do NOT create new ones unless none fit.

## DI

DI from start. Never `new` service inside class. Never modify production code solely for testing — implement interfaces in test project instead.

## Architecture

- `Directory.Build.props/targets` and `Directory.Packages.props` centralized — never duplicate in `.csproj`.
- Child `Directory.Build.props` MUST import parent via `$([MSBuild]::GetPathOfFileAbove(...))`.
- All `bin/` and `obj/` redirect to `artifacts/`.
- Local dev: `<ProjectReference>` not `<PackageReference>` (avoids publish cycles).
- CI version: `-p:Version=$(GitTag)`; local fallback `0.1.0`. Tag format: `v1.2.3`.

## MANDATORY Rules

- **Async methods** MUST end `Async` — exceptions: EventHandlers, tests. Neither get suffix.
- **Blank line before `return`** after code block. NOT after `if`/`else`.
- **ALL** new code needs GH issue (create if missing), must use TDD — failing test committed first (red), confirm fail, then implement + commit production code separately (green). Never batch test + production code one commit. New Git branch required: `feature/<gh-issue-number-if-available>-short-description` / `bug/<gh-issue-number-if-available>-short-description` / etc.
- **Coverage exclusions** — class not testable/little regression value: add `[ExcludeFromCodeCoverage]`
- **PR** Development done → push branch, raise PR, request human review
- **NEVER** touch code unrelated to requested change (no judgement-call restructuring, reordering, "while I'm here" cleanup). Beneficial-but-unrelated change (logical grouping, indirect refactor, etc.) → SUGGEST as separate item, don't implement.
- **Test projects**: `*.Tests.Unit` / `*.Tests.Integration`
- **Method signatures**: single-line regardless param count. Split only >200 chars.
- **Commits**: Conventional Commits — `feat(scope): ...`, `fix(scope): ...`
- **Branches**: `feature/...`, `bug/...`, `fix/...`, `doc/...`; `main` always deployable
- **Comments**: never comment inside methods
- **XML comments**: all public members. Implementing interface → `<inheritdoc />` only.
- **Error handling**: public APIs never throw for invalid input — use `Result<T>` or normalize gracefully. See @.claude/rules/c-sharp-code-style.md § Error Handling.

Patterns: see @.claude/rules/c-sharp-code-style.md and @.claude/rules/avalonia-ui.md.

## Plans

Plan approved → raise one GitHub issue per phase before writing code.

## Before Starting ANY Task (mandatory, no exceptions)

1. **Repo + folder** — run `gh repo view --json nameWithOwner -q '.nameWithOwner'`, confirm correct `src/` folder for issue scope.
2. **Branch** — confirm not on `main`. Create branch first.
3. **TDD** — commit failing test BEFORE production code.
4. **Scope** — implement only what asked. Stop for review before touching other areas.

## Code Exploration

- Call Serena `initial_instructions` BEFORE exploring — no exceptions.
- Use `mcp__serena__find_symbol` / `mcp__serena__find_referencing_symbols` — do NOT read whole files.
- Cap 5 file reads before stating plan. Don't keep reading without producing fix.
- Find ALL call sites and test files before touching production code.
- Read file before editing. Grep all callers before modifying function.

## Definition of Done

1. `dotnet clean && dotnet build --no-restore` — zero errors, zero warnings. Paste exact output. `dotnet clean` mandatory — incremental cache hides analyser warnings.
2. `dotnet test --no-build` — paste EXACT pass/fail count. New failures = zero. Change broke tests → diagnose + fix, never dismiss as pre-existing.
3. **Leave better than found.** Pre-existing failures in any touched/reported test project must get fixed same branch, separate commit — never merged around, never left red. Unless failure exposes genuine production bug (raise before changing anything), production behaviour is spec — update test to match. Test for deleted feature gets deleted, not skipped.
4. Confirm all call sites and test files found and updated.
5. Commit to branch (not main).
6. Push branch, **raise PR using `.github/PULL_REQUEST_TEMPLATE.md`** — fill all sections, don't omit or rewrite. Human review happens on PR, not before commit/push.

Never say "fixed"/"done" without evidence. Say "I believe this is fixed because…"
Sync/download bugs: confirm full flow (Graph API → persistence → sync logic) first.

## GitHub

Always use `gh` CLI for all GitHub operations. Never use MCP GitHub — not configured.

## Subagents

- `c-sharp-qa` → tests; `c-sharp-dev` → C# features; `c-sharp-reviewer` → code review.
- After any subagent: `Read` every claimed file, re-run `dotnet test` yourself, verify diff.
- Subagent drifts → take over directly, don't re-prompt.

## graphify

Project has knowledge graph at graphify-out/ — god nodes, community structure, cross-file relationships.

Rules:

- Codebase questions: run `graphify query "<question>"` first when graphify-out/graph.json exists. Use `graphify path "<A>" "<B>"` for relationships, `graphify explain "<concept>"` for focused concepts. Returns scoped subgraph, usually much smaller than GRAPH_REPORT.md or raw grep output.
- graphify-out/wiki/index.md exists → use for broad navigation instead of raw source browsing.
- Read graphify-out/GRAPH_REPORT.md only for broad architecture review or when query/path/explain don't surface enough context.
- After modifying code, run `graphify update .` to keep graph current (AST-only, no API cost).
