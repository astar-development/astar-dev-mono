# AStar.Dev.Utilities

A collection of general-purpose extension methods and utilities for .NET applications. Designed to make common operations more fluent and readable without pulling in heavier dependencies.

[![NuGet](https://img.shields.io/nuget/v/AStar.Dev.Utilities)](https://www.nuget.org/packages/AStar.Dev.Utilities)
[![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg)](LICENSE)

---

## Installation

```bash
dotnet add package AStar.Dev.Utilities
```

Or via the NuGet Package Manager in Visual Studio / Rider.

---

## Available extensions

| Class | Description | Reference |
|-------|-------------|-----------|
| [`Constants`](docs/Constants.md) | Shared `JsonSerializerOptions` instances | [docs](docs/Constants.md) |
| [`EncryptionExtensions`](docs/EncryptionExtensions.md) | AES encrypt / decrypt strings | [docs](docs/EncryptionExtensions.md) |
| [`EnumExtensions`](docs/EnumExtensions.md) | Parse a string to an enum value | [docs](docs/EnumExtensions.md) |
| [`LinqExtensions`](docs/LinqExtensions.md) | `ForEach` for `IEnumerable<T>` | [docs](docs/LinqExtensions.md) |
| [`ObjectExtensions`](docs/ObjectExtensions.md) | Serialize any object to JSON | [docs](docs/ObjectExtensions.md) |
| [`RegexExtensions`](docs/RegexExtensions.md) | Character-class validation helpers | [docs](docs/RegexExtensions.md) |
| [`StringExtensions`](docs/StringExtensions.md) | Null checks, JSON round-trip, string manipulation | [docs](docs/StringExtensions.md) |

---

## Build

This package lives inside the [astar-dev-mono](https://github.com/astar-development/astar-dev-mono) mono-repo and inherits all build configuration from the root `Directory.Build.props`.

```bash
# From the repo root — builds everything
dotnet build

# Build only this package
dotnet build packages/core/AStar.Dev.Utilities

# If Directory.Build.props changes aren't being picked up
dotnet clean && dotnet build packages/core/AStar.Dev.Utilities
```

---

## Test

Tests for this package live under `tests/` at the repo root.

```bash
# Run all tests
dotnet test

# Run tests for this package specifically
dotnet test tests/AStar.Dev.Utilities.Tests
```

---

## Contributing

1. Fork the repo and create a branch: `feat/utilities-<short-description>` or `fix/utilities-<short-description>`.
2. Follow the [Conventional Commits](https://www.conventionalcommits.org/) format — e.g. `feat(packages/core/AStar.Dev.Utilities): add XyzExtensions`.
3. All warnings are treated as errors (`TreatWarningsAsErrors=true`), so the build must stay clean.
4. Add or update the relevant doc file under `docs/` if you add a new extension or change an existing one.
5. Open a PR against `main`. CI runs automatically on push.

Do **not** run `dotnet pack` or `dotnet nuget push` manually — releases are triggered by pushing a `v*` tag. See the repo-level [Releasing a new version](../../../docs/guides/releasing-a-new-version.md) guide.
