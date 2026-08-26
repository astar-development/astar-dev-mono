# AStarDev.LoggingOTel

OpenTelemetry logging configuration helper: console exporter + Azure Monitor (Application Insights) log exporter, driven by `IConfigurationRoot`.

[![NuGet](https://img.shields.io/nuget/v/AStarDev.LoggingOTel)](https://www.nuget.org/packages/AStarDev.LoggingOTel)
[![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg)](LICENSE)

---

## Installation

```bash
dotnet add package AStarDev.LoggingOTel
```

Or via the NuGet Package Manager in Visual Studio / Rider.

---

## Available extensions

`ILoggingBuilder.ConfigureOTelLogging(configuration, connectionStringConfigurationKey = "ApplicationInsights:ConnectionString")`

Wires up OpenTelemetry logging that:

- writes to the console
- exports to Azure Monitor when a connection string is present at `connectionStringConfigurationKey` in the supplied `IConfigurationRoot`
- includes formatted messages, scopes, and structured state values

An overload additionally accepts an `InMemoryLogProcessor`, routing log records to it as well — used for the in-app log viewer feature (`AStarDev.LoggingOTel.LogViewer`).

---

## Build

This package lives inside the [astar-dev-mono](https://github.com/astar-development/astar-dev-mono) mono-repo and inherits all build configuration from the root `Directory.Build.props`.

```bash
# From the repo root — builds everything
dotnet build

# Build only this package
dotnet build packages/core/logging/AStarDev.LoggingOTel

# If Directory.Build.props changes aren't being picked up
dotnet clean && dotnet build packages/core/logging/AStarDev.LoggingOTel
```

---

## Test

Tests for this package live alongside it in `AStarDev.LoggingOTel.TestsUnit`.

```bash
# Run all tests
dotnet test

# Run tests for this package specifically
dotnet test packages/core/logging/AStarDev.LoggingOTel.TestsUnit
```

---

## Contributing

1. Fork the repo and create a branch: `feat/logging-otel-<short-description>` or `fix/logging-otel-<short-description>`.
2. Follow the [Conventional Commits](https://www.conventionalcommits.org/) format — e.g. `feat(packages/core/logging/AStarDev.LoggingOTel): add XyzExtensions`.
3. All warnings are treated as errors (`TreatWarningsAsErrors=true`), so the build must stay clean.
4. Add or update the relevant doc file under `docs/` if you add a new extension or change an existing one.
5. Open a PR against `main`. CI runs automatically on push.

Do **not** run `dotnet pack` or `dotnet nuget push` manually — releases are triggered by pushing a `v*` tag. See the repo-level [Releasing a new version](../../../docs/guides/releasing-a-new-version.md) guide.
