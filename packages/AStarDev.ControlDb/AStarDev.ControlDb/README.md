# AStarDev.ControlDb

This is an internal database used by several applications. If it is any use to you, please feel free!

[![NuGet](https://img.shields.io/nuget/v/AStarDev.ControlDb)](https://www.nuget.org/packages/AStarDev.ControlDb)
[![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg)](LICENSE)

---

## Installation

```bash
dotnet add package AStarDev.Utilities
```

Or via the NuGet Package Manager in Visual Studio / Rider.

---

## Build

This package lives inside the [astar-dev-mono](https://github.com/astar-development/astar-dev-mono) mono-repo and inherits all build configuration from the root `Directory.Build.props`.

```bash
# From the repo root — builds everything
dotnet build

# Build only this package
dotnet build packages/AStarDev.ControlDb/AStarDev.ControlDb

# If Directory.Build.props changes aren't being picked up
dotnet clean && dotnet build packages/AStarDev.ControlDb/AStarDev.ControlDb
```

---

## Test

Tests for this package live next to the project.

```bash
# Run all tests
dotnet test

# Run tests for this package specifically
dotnet test packages/AStarDev.ControlDb/AStarDev.ControlDb.Tests.Unit/AStarDev.ControlDb.Tests.Unit.csproj
or
dotnet test packages/AStarDev.ControlDb/AStarDev.ControlDb.Tests.Integration/AStarDev.ControlDb.Tests.Integration.csproj
```

---

## Contributing

1. Fork the repo and create a branch: `feat/utilities-<short-description>` or `fix/utilities-<short-description>`.
2. Follow the [Conventional Commits](https://www.conventionalcommits.org/) format — e.g. `feat(packages/core/AStarDev.ControlDb): add XyzExtensions`.
3. All warnings are treated as errors (`TreatWarningsAsErrors=true`), so the build must stay clean.
4. Add or update the relevant doc file under `docs/` if you add a new extension or change an existing one.
5. Open a PR against `main`. CI runs automatically on push.

Do **not** run `dotnet pack` or `dotnet nuget push` manually — releases are triggered by pushing a `v*` tag. See the repo-level [Releasing a new version](../../../docs/guides/releasing-a-new-version.md) guide.
