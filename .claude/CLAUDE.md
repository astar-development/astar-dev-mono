# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Repository Overview

Mono-repo containing all AStar Development products: **Blazor** web apps, **Next.js** web apps, **Avalonia** desktop apps, and ~50 published **NuGet packages**. Solution file: `MonoRepo.slnx`.

## Build Commands

```bash
# Restore all .NET dependencies
dotnet restore

# Build all projects (Debug)
dotnet build

# Build release
dotnet build --configuration Release

# Build a specific project
dotnet build packages/core/[PackageName]
dotnet build apps/web/[AppName].Blazor

# If Directory.Build.props changes aren't taking effect
dotnet clean && dotnet build
```

## Test Commands

```bash
# Run all tests
dotnet test

# Run tests for a specific project
dotnet test tests/[ProjectName].Tests

# Run with coverage
dotnet test --collect:"XPlat Code Coverage" -- DataCollectionRunSettings.DataCollectors.DataCollector.Configuration.Format=cobertura
```

Coverage reports are written to `TestResults/`.

## Running Applications

```bash
# Blazor app
dotnet run --project apps/web/[AppName].Blazor

# Avalonia desktop app
dotnet run --project apps/desktop/[AppName].Desktop

# Next.js app
cd apps/web/[appname]-next && npm run dev
```

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
- During local development, prefer `<ProjectReference>` over `<PackageReference>` to avoid publish cycles

### CI/CD Workflows

| Workflow | Trigger |
|----------|---------|
| `dotnet-ci.yml` | Push/PR touching `.cs`, `.csproj`, `*.slnx`, MSBuild config |
| `nuget-publish.yml` | Push of a `v*` tag |
| `infra-deploy.yml` | Push/PR touching `infra/**` |

### Conventions

- **Commit messages**: Conventional Commits format — `feat(packages/core): ...`, `fix(apps/web/Portal.Blazor): ...`
- **Branch names**: `feature/...`, `bug/...`, `doc/...`; `main` is always deployable
- **Test projects**: Named `*.Tests` or `*.IntegrationTests` — automatically set `IsPackable=false`
- **Method signatures**: Always single-line regardless of parameter count — `public void Foo(string a, int b, CancellationToken ct = default)`. Never split parameters across lines.
- **Child `Directory.Build.props`**: Sub-folder overrides must import the parent via `$([MSBuild]::GetPathOfFileAbove(...))`

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

## Definition of Done

Before considering any coding task complete — including commits and PRs — always:

1. `dotnet build` the affected project(s) and confirm zero errors and zero warnings
2. `dotnet test` the affected test project(s) and confirm all tests pass

Do not raise a PR or claim a task is finished until both steps pass locally.

## Additional Instructions

- Git workflow: @docs/git-instructions.md
