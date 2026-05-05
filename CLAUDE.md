# CLAUDE.md

Guidance to Claude Code (claude.ai/code) for this repo.

## Repository Overview

Mono-repo, all AStar Development products: **Blazor** web apps, **astro** web apps, **Avalonia** desktop apps, ~25 published **NuGet packages**. Solution: `AStar.Dev.slnx`.

## Build Commands

```bash
dotnet restore
dotnet build
dotnet build packages/core/[PackageName]
dotnet build apps/web/[AppName].Blazor

# If Directory.Build.props changes aren't taking effect
dotnet clean && dotnet build

# If build fails mysteriously after a refactor (stale generated files, changed base props)
dotnet clean && rm -rf artifacts/ && dotnet build
```

## Test Commands

```bash
dotnet test

dotnet test tests/[ProjectName].Tests
```

Coverage → `TestResults/`.

## Running Applications

```bash
dotnet run --project apps/web/[AppName].Blazor

dotnet run --project apps/desktop/[AppName].Desktop

cd apps/web/[appname] && npm run dev
```

## Logging

Logging NEVER afterthought. ALL logging MUST go to Azure Application Insights unless instructed otherwise. For C#, use `AStar.Dev.Logging.Extensions` (`LogMessage` class) for compile-time templates. No suitable template? ADD IT. Avoid `logger.Log...`. No JS equivalent exists; log anyway.

## Dependency Injection

DI NEVER afterthought. Language supports it? MUST implement from start.

## NuGet projects

Mono-repo has many C# NuGet projects deployed to GitHub and NuGet.org. Top 3 that MUST be used:

- AStar.Dev.Functional.Extensions: Result<T>, Option<T>, Map<Async>, Bind<Async> etc. ALWAYS use when practicable.
- AStar.Dev.Logging.Extensions: Compile-time `LogMessage` format + logging extensions. ALWAYS use when practicable.
- AStar.Dev.Utilities: string extensions, nullability checks, utility methods. ALWAYS use when practicable.

Suitable method exists in above? USE IT. New reusable code? ADD IT to relevant project + raise GitHub issue documenting why, where, how to deploy.

## Architecture

### Directory Layout

```
apps/
  web/        # Blazor (C#) WebAssembly/Server and astro apps
  desktop/    # Avalonia (C#) cross-platform desktop apps
packages/
  core/       # Domain models, business logic
  infra/      # Data access, auth, logging, HTTP clients
  ui/         # Shared UI components
infra/terraform/
  staging/    # Auto-applies on merge to main
  prod/       # Requires GitHub Environments approval gate
tests/        # Repo-wide integration/E2E tests
```

### C# Centralized Build Configuration

All `.csproj` inherit from three root files — never duplicate settings already defined there:

- **`Directory.Build.props`**
- **`Directory.Build.targets`**
- **`Directory.Packages.props`**

### Build Output

ALL `bin/` and `obj/` redirect to `artifacts/` at repo root.

### Versioning

- CI injects version at publish via `-p:Version=$(GitTag)`; local builds fallback to `0.1.0`
- Tag format: `v1.2.3` (triggers `nuget-publish.yml`)
- Pre-release: `v2.0.0-beta.1`

### NuGet Packages

- Naming: `AStar.Dev.[Area].[Name]`
- Published to GitHub Packages and NuGet.org via CI
- Symbol packages (`.snupkg`) published alongside `.nupkg`
- **No manual release** — use tag workflow
- Local dev: use `<ProjectReference>` not `<PackageReference>` to avoid publish cycles

### Avalonia / Blazor / C# / .NET Patterns

C#-specific patterns (DI, EF Core, Mediator/MediatR, Avalonia, Refit/Polly, Serilog, FluentValidation, functional extensions): see @.claude/agents/c-sharp-senior-developer.md and @.claude/rules/c-sharp-code-style.md.
C# updates: use @.claude/agentsc-sharp-senior-developer.md subagent.

### C#/.NET Conventions

- Eliminate "what" comments by extracting well-named methods — NOT by moving them into XML docs.
- Blank line before every `return` (except `return` directly after `if`/`else`).

### Conventions

- **Commit messages**: Conventional Commits — `feat(packages/core): ...`, `fix(apps/web/Portal.Blazor): ...`
- **Branch names**: `feature/...`, `bug/...`, `doc/...`; `main` ALWAYS deployable
- **Test projects**: Named `*.Tests.Unit` or `*.Tests.Integration` — auto-set `IsPackable=false`
- **Method signatures**: Always single-line regardless of param count — `public void Foo(string a, int b, CancellationToken ct = default)`. Never split params across lines. Every file type.
- **Comments**: Never restate what code says — any file type (`.cs`, `.csproj`, `.axaml`, config, etc.). Refactor to extract when needed. Only comment when _reason_ behind decision isn't derivable from code.
- **Child `Directory.Build.props`**: Sub-folder overrides must import parent via `$([MSBuild]::GetPathOfFileAbove(...))`
- **Naming**: follow @.claude/rules/c-sharp-code-style.md (.Net) or @.claude/rules/javascript-code-style.md (JS). Fallback to official language naming when no local spec exists.
- **XML Comments**: all public methods/properties
    - Classes implementing interface: use `<inheritdoc />`, not class-level docs.

## Before Starting ANY Task

Two steps **MANDATORY** before single line of code. No exceptions, including spikes.

1. **Branch first** — run `git branch`, confirm not on `main`. If on main, create branch:

    ```bash
    git checkout -b feature/short-description<-issue-number>
    ```

    Naming: `feature/...`, `bug/...`, `doc/...`. NEVER commit to `main`. See @docs/git-instructions.md.

2. **Tests MANDATORY** — EVERY coding task MUST follow TDD. COMMIT FAILING TEST BEFORE writing production code.

## Branching & Commits

ALL development work MUST follow the GIT rules in: @docs/git-instructions.md

## Definition of Done

Before any coding task complete — commits and PRs included:

1. `dotnet build` affected projects — zero errors, zero warnings
2. `dotnet test` affected test projects — all pass except new TDD `RED` tests. COMMIT failing tests.
3. Write MINIMAL production code to pass test(s)
3. Request human review BEFORE committing.
4. Human requests changes? Implement, re-request review.
5. ONLY after human approval: commit to branch, raise GitHub PR.

## Verification Before Declaring Done

NEVER say "fixed", "done", or "complete" without explicit evidence:

- Run `dotnet build` — zero errors required.
- Run `dotnet test` — zero failures (excluding committed RED tests).
- Trace the original bug/requirement through the code path and state in plain text WHY the change addresses it at the root cause.
- For sync/download bugs specifically: confirm the full flow (Graph API → persistence → sync logic) before touching any code. Write a failing reproducing test first; declare done only when it turns green.

Say "I believe this is fixed because…" — never just "fixed".

## Subagent Usage

- Use `c-sharp-qa` subagent for adding or expanding tests in C# files.
- Use `c-sharp-dev` subagent for implementing C# features.
- Use `c-sharp-reviewer` subagent for code review.
- When a subagent drifts off task or produces wrong output, take over directly — do not re-prompt the same agent repeatedly.

## graphify

This project has a graphify knowledge graph at graphify-out/.

Rules:
- Before answering architecture or codebase questions, read graphify-out/GRAPH_REPORT.md for god nodes and community structure
- If graphify-out/wiki/index.md exists, navigate it instead of reading raw files
- For cross-module "how does X relate to Y" questions, prefer `graphify query "<question>"`, `graphify path "<A>" "<B>"`, or `graphify explain "<concept>"` over grep — these traverse the graph's EXTRACTED + INFERRED edges instead of scanning files
- After modifying code files in this session, run `graphify update .` to keep the graph current (AST-only, no API cost)
