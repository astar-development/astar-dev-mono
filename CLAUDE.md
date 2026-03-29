# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Repository Overview

Mono-repo containing all AStar Development products: **Blazor** web apps, **astro** web apps, **Avalonia** desktop apps, and ~25 published **NuGet packages**. Solution file: `MonoRepo.slnx`.

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

Coverage reports are written to `TestResults/`.

## Running Applications

```bash
dotnet run --project apps/web/[AppName].Blazor

dotnet run --project apps/desktop/[AppName].Desktop

cd apps/web/[appname] && npm run dev
```

## Logging

Logging is NEVER an after-thought. Whether .Net or JavaScript, logging MUST be implemented from day one. Refer to the @.claude/agents/c-sharp-senior-developer.md for all .Net Projects or @.claude/agents/javascript-senior-developer.md.md for JavaScript projects for additional logging information. ALL Logging MUST be sent to Azure Application Insights unless otherwise instructed. Where applicable, use the AStar.Dev.Logging.Extensions (specifically the `LogMessage` class) to use compile-time logging templates. If a suitable template does not exist, ADD IT. Avoid `logger.Log...` wherever possible. There is currently no JavaScript equivalent but logging must still be performed.

## Dependency Injection

Dependency Injection is NEVER an after-thought. If the language supports it, DI MUST be implemented from the beginning.

## NuGet projects

The mono repo contains many NuGet projects that are deployed to both GitHub and NuGet. These include:

- AStar.Dev.Functional.Extensions: Result<T>, Option<T>, Map<Async>, Bind<Async> etc to support the functional approach. ALWAYS use when practicable.
- AStar.Dev.Logging.Extensions: Compile-time LogMessage format and many other related logging extensions. ALWAYS use when practicable.
- AStar.Dev.Utilities: a plethora of string extensions, nullability checks and many other `utility` methods. ALWAYS use when practicable.

- If the above (or the other projects) have a suitable method: USE IT. If new code is reusable: ADD IT to the relevant project and raise a GitHub issue documenting why it was created, where it was created and how to deploy the updated package to GitHub / NuGet.

## Architecture

### Directory Layout

```
apps/
  web/        # Blazor WebAssembly/Server and Next.js apps
  desktop/    # Avalonia cross-platform desktop apps
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

All `.csproj` files automatically inherit from three root files — never duplicate settings already defined there:

- **`Directory.Build.props`** — Target framework (`net10.0`), nullable reference types, `TreatWarningsAsErrors=true`, output paths to `artifacts/`, shared NuGet metadata
- **`Directory.Build.targets`** — Source Link, package metadata validation (requires `Description`, `PackageTags`, `PackageLicenseExpression`), project naming enforcement (`AStar.Dev.*` prefix), VSTest blame mode
- **`Directory.Packages.props`** — Central Package Management (CPM): all NuGet package versions are declared here; individual `.csproj` files reference packages without versions

### Build Output

All `bin/` and `obj/` folders redirect to `artifacts/` at the repo root. Do not look for binaries inside individual project directories.

### Versioning

- Version is injected by CI at publish time via `-p:Version=$(GitTag)`; local builds use the fallback `0.0.1-local`
- Single repo-wide version tag: all packages in a release share the same version
- Tag format: `v1.2.3` (triggers `nuget-publish.yml`)
- Pre-release: `v2.0.0-beta.1`

### NuGet Packages

- Naming convention: `AStar.Dev.[Area].[Name]`
- Published to both GitHub Packages and NuGet.org via CI
- Symbol packages (`.snupkg`) are published alongside `.nupkg`
- **Do not run `dotnet pack` / `dotnet nuget push` manually for releases** — use the tag workflow
- During local development, use `<ProjectReference>` instead of `<PackageReference>` to avoid publish cycles

### CI/CD Workflows

| Workflow            | Trigger                                                     |
| ------------------- | ----------------------------------------------------------- |
| `dotnet-ci.yml`     | Push/PR touching `.cs`, `.csproj`, `*.slnx`, MSBuild config |
| `nuget-publish.yml` | Push of a `v*` tag                                          |
| `infra-deploy.yml`  | Push/PR touching `infra/**`                                 |

### Conventions

- **Commit messages**: Conventional Commits format — `feat(packages/core): ...`, `fix(apps/web/Portal.Blazor): ...`
- **Branch names**: `feature/...`, `bug/...`, `doc/...`; `main` is always deployable
- **Test projects**: Named `*.Tests` or `*.IntegrationTests` — automatically set `IsPackable=false`
- **Method signatures**: Always single-line regardless of parameter count — `public void Foo(string a, int b, CancellationToken ct = default)`. Never split parameters across lines.
- **Child `Directory.Build.props`**: Sub-folder overrides must import the parent via `$([MSBuild]::GetPathOfFileAbove(...))`
- **Naming Conventions**: follow the @.claude/rules/c-sharp-code-style.md for .Net projects or @.claude/rules/avscrpt-code-style.md for .Net projects for JavaScript projects. Fallback to the official language-specific naming when no local specification exists locally.

## First-Time Setup

GitHub Packages authentication (one-time, writes to `~/.nuget/NuGet/NuGet.Config`):

```bash
dotnet nuget add source \
  --username YOUR_GITHUB_USERNAME \
  --password YOUR_PAT_TOKEN \
  --store-password-in-clear-text \
  --name github \
  "https://nuget.pkg.github.com/astar.development/index.json"
```

A PAT with `read:packages` scope is required. If `dotnet restore` fails with 401, the PAT has expired — re-run this command with a fresh token.

See @README.md for project overview and @package.json for available npm commands.

## Before Starting Any Task

These two steps are **mandatory** before writing a single line of code. No exceptions, including spikes.

1. **Branch first** — run `git branch` to confirm you are not on `main`. If you are, create a feature branch immediately:
   ```bash
   git checkout -b feature/short-description
   ```
   Branch naming: `feature/...`, `bug/...`, `doc/...`. Never commit directly to `main`. See @docs/git-instructions.md.

2. **Tests first (TDD)** — every coding task requires a test project. Write a failing test before writing any production code. Follow the red → green → refactor commit sequence defined in @.claude/agents/c-sharp-senior-qa-specialist.md (C#) or @.claude/agents/javascript-senior-qa-specialist.md (JavaScript/TypeScript).

## Definition of Done

Before considering any coding task complete — including commits and PRs — always:

1. `dotnet build` the affected project(s) and confirm zero errors and zero warnings
2. `dotnet test` the affected test project(s) and confirm all tests pass
3. Request a review from the @.claude/agents/c-sharp-senior-qa-specialist.md for all .Net Projects or @.claude/agents/javascript-senior-qa-specialist.md for JavaScript projects
4. Resolve ALL `Blocker` and `Major` issues the QA agent reports. Raise GitHub issues for all other issues so they can be addressed later.
5. Repeat the QA loop until there are no `Blocker` or `Major` isues from the original code or any updates.

Do not raise a PR or claim a task is finished until both steps pass locally.

## Additional Instructions

- Git workflow: @docs/git-instructions.md
