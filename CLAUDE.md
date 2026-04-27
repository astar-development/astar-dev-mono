# CLAUDE.md

Guidance to Claude Code (claude.ai/code) for this repo.

## Repository Overview

Mono-repo, all AStar Development products: **Blazor** web apps, **astro** web apps, **Avalonia** desktop apps, ~25 published **NuGet packages**. Solution: `AStar.Dev.slnx`.

## Build Commands

```bash
dotnet restore

dotnet build
or
dotnet build --configuration Release

dotnet build packages/core/[PackageName]
dotnet build apps/web/[AppName].Blazor

# If Directory.Build.props changes aren't taking effect
dotnet clean && dotnet build
```

## Test Commands

```bash
dotnet test

dotnet test tests/[ProjectName].Tests

dotnet test --collect:"XPlat Code Coverage" -- DataCollectionRunSettings.DataCollectors.DataCollector.Configuration.Format=cobertura
```

Coverage → `TestResults/`.

## Running Applications

```bash
dotnet run --project apps/web/[AppName].Blazor

dotnet run --project apps/desktop/[AppName].Desktop

cd apps/web/[appname] && npm run dev
```

## Logging

Logging NEVER afterthought. .Net or JavaScript — MUST implement day one. See @.claude/agents/c-sharp-senior-developer.md (.Net) or @.claude/agents/javascript-senior-developer.md.md (JS). ALL logging MUST go to Azure Application Insights unless instructed otherwise. Use `AStar.Dev.Logging.Extensions` (`LogMessage` class) for compile-time templates. No suitable template? ADD IT. Avoid `logger.Log...`. No JS equivalent exists; log anyway.

## Dependency Injection

DI NEVER afterthought. Language supports it? MUST implement from start.

## NuGet projects

Mono-repo has many NuGet projects deployed to GitHub and NuGet.org:

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

### Centralized Build Configuration

All `.csproj` inherit from three root files — never duplicate settings already defined there:

- **`Directory.Build.props`** — Target framework (`net10.0`), nullable reference types, `TreatWarningsAsErrors=true`, output paths to `artifacts/`, shared NuGet metadata
- **`Directory.Build.targets`** — Source Link, package metadata validation (requires `Description`, `PackageTags`, `PackageLicenseExpression`), project naming enforcement (`AStar.Dev.*` prefix), VSTest blame mode
- **`Directory.Packages.props`** — Central Package Management (CPM): all NuGet versions declared here; `.csproj` files reference without versions

### Build Output

All `bin/` and `obj/` redirect to `artifacts/` at repo root. Don't look for binaries inside individual project dirs.

### Versioning

- CI injects version at publish via `-p:Version=$(GitTag)`; local builds fallback to `0.0.1-local`
- Single repo-wide version tag: all packages in release share same version
- Tag format: `v1.2.3` (triggers `nuget-publish.yml`)
- Pre-release: `v2.0.0-beta.1`

### NuGet Packages

- Naming: `AStar.Dev.[Area].[Name]`
- Published to GitHub Packages and NuGet.org via CI
- Symbol packages (`.snupkg`) published alongside `.nupkg`
- **No manual `dotnet pack` / `dotnet nuget push` for releases** — use tag workflow
- Local dev: use `<ProjectReference>` not `<PackageReference>` to avoid publish cycles

### Avalonia / Blazor / C# / .NET Patterns

C#-specific patterns (DI, EF Core, Mediator/MediatR, Avalonia, Refit/Polly, Serilog, FluentValidation, functional extensions): see @.claude/agents/c-sharp-senior-developer.md and @.claude/rules/c-sharp-code-style.md.
C# updates: use @.claude/agentsc-sharp-senior-developer.md subagent.

### C#/.NET Conventions

- Eliminate "what" comments by extracting well-named methods — NOT by moving them into XML docs.
- Blank line before every `return` (except `return` directly after `if`/`else`).
- ReactiveUI issues: prefer `RxAppBuilder`/`RxSchedulers` over NuGet downgrades.
- Testing internals with NSubstitute: account for proxy limitations on `internal` classes.

### CI/CD Workflows

| Workflow            | Trigger                                                     |
| ------------------- | ----------------------------------------------------------- |
| `dotnet-ci.yml`     | Push/PR touching `.cs`, `.csproj`, `*.slnx`, MSBuild config |
| `nuget-publish.yml` | Push of a `v*` tag                                          |
| `infra-deploy.yml`  | Push/PR touching `infra/**`                                 |

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

## Before Starting Any Task

Two steps **MANDATORY** before single line of code. No exceptions, including spikes.

1. **Branch first** — run `git branch`, confirm not on `main`. If on main, create branch:

    ```bash
    git checkout -b feature/short-description
    ```

    Naming: `feature/...`, `bug/...`, `doc/...`. Never commit to `main`. See @docs/git-instructions.md.

2. **Tests MANDATORY** — every coding task needs test project. New code MUST have tests covering all branches wherever possible.

## Branching & Commits

- NEVER commit to `main`. Always branch first.
- NEVER commit code with failing tests or build errors.
- Human signals completion (`perfect`, `thanks`, `lgtm`) → STOP editing unless asked.

## Workflow Discipline

- Non-trivial change? State plan, confirm with human first.
- Don't spend entire session exploring. Timebox, then implement.
- Verify shell working directory before creating migrations or path-sensitive artifacts.
- EF Core migrations: always include Designer file.

## Definition of Done

Before any coding task complete — commits and PRs included:

1. `dotnet build` affected projects — zero errors, zero warnings
2. `dotnet test` affected test projects — all pass
3. Request human review BEFORE committing.
4. Human requests changes? Implement, re-request review.
5. ONLY after human approval: commit to branch, raise GitHub PR.

## Additional Instructions

- Git workflow: @docs/git-instructions.md