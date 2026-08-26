# AStarDev.LoggingSerilog

Serilog logger configuration helper: console + rolling JSON file sinks, driven by `IConfigurationRoot`.

[![NuGet](https://img.shields.io/nuget/v/AStar.Dev.LoggingSerilog)](https://www.nuget.org/packages/AStar.Dev.LoggingSerilog)
[![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg)](LICENSE)

---

## Installation

```bash
dotnet add package AStarDev.LoggingSerilog
```

Or via the NuGet Package Manager in Visual Studio / Rider.

---

## Available extensions

`SerilogConfigurator.CreateLogger(configuration, logFilePath, rollingInterval = Day, retainedFileCountLimit = 7)`

Builds an `ILogger` that:

- reads sink/level settings from the supplied `IConfigurationRoot` (`ReadFrom.Configuration`)
- writes to the console (invariant culture format)
- writes JSON-formatted log events to `logFilePath`, rolling on `rollingInterval` and keeping `retainedFileCountLimit` files, shared across processes, flushed to disk every second

---

## Build

This package lives inside the [astar-dev-mono](https://github.com/astar-development/astar-dev-mono) mono-repo and inherits all build configuration from the root `Directory.Build.props`.

```bash
# From the repo root — builds everything
dotnet build

# Build only this package
dotnet build packages/core/logging/AStarDev.LoggingSerilog

# If Directory.Build.props changes aren't being picked up
dotnet clean && dotnet build packages/core/logging/AStarDev.LoggingSerilog
```

---

## Test

Tests for this package live alongside it in `AStarDev.LoggingSerilog.TestsUnit`.

```bash
# Run all tests
dotnet test

# Run tests for this package specifically
dotnet test packages/core/logging/AStarDev.LoggingSerilog.TestsUnit
```

---

## Contributing

1. Fork the repo and create a branch: `feat/logging-serilog-<short-description>` or `fix/logging-serilog-<short-description>`.
2. Follow the [Conventional Commits](https://www.conventionalcommits.org/) format — e.g. `feat(packages/core/logging/AStarDev.LoggingSerilog): add XyzExtensions`.
3. All warnings are treated as errors (`TreatWarningsAsErrors=true`), so the build must stay clean.
4. Add or update the relevant doc file under `docs/` if you add a new extension or change an existing one.
5. Open a PR against `main`. CI runs automatically on push.

Do **not** run `dotnet pack` or `dotnet nuget push` manually — releases are triggered by pushing a `v*` tag. See the repo-level [Releasing a new version](../../../docs/guides/releasing-a-new-version.md) guide.
