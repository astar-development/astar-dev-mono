# AStar.Dev.Utilities

> Extension methods, helpers, and shared primitives for .NET 10 — the common
> foundation used across all AStar Development packages and applications.

[![NuGet](https://img.shields.io/nuget/v/AStar.Dev.Utilities.svg)](https://www.nuget.org/packages/AStar.Dev.Utilities)
[![NuGet Downloads](https://img.shields.io/nuget/dt/AStar.Dev.Utilities.svg)](https://www.nuget.org/packages/AStar.Dev.Utilities)
[![CI](https://github.com/your-org/your-repo/actions/workflows/dotnet-ci.yml/badge.svg)](https://github.com/your-org/your-repo/actions/workflows/dotnet-ci.yml)
[![License: MIT](https://img.shields.io/badge/License-MIT-blue.svg)](LICENSE.txt)

---

## Contents

- [Installation](#installation)
- [Quick start](#quick-start)
- [Usage](#usage)
- [Compatibility](#compatibility)
- [Changelog](#changelog)
- [Contributing](#contributing)

---

## Installation

```bash
dotnet add package AStar.Dev.Utilities
```

If consuming from **GitHub Packages**, ensure your `NuGet.Config` includes
the GitHub feed and you have a PAT with `read:packages` scope. See the
[repo README](../../README.md#first-time-setup) for the one-time setup steps.

---

## Quick start

```csharp
// All utilities are available via their respective namespaces.
// No registration or configuration step is required —
// this package contains pure utility code with no DI dependencies.
using AStar.Dev.Utilities;

// Example — replace with a real usage example from your codebase
var result = "hello world".ToTitleCase();
```

---

## Usage

<!--
  Add a section per logical group of utilities in this package.
  Examples below are placeholders — replace with real API surface.
-->

### String extensions

```csharp
// Replace with real examples from the package
var titled  = "hello world".ToTitleCase();   // "Hello World"
var trimmed = "  value  ".NullIfWhiteSpace(); // null
```

### Collection helpers

```csharp
// Replace with real examples from the package
var items = new List<string> { "a", "b", "c" };
var batches = items.Batch(size: 2); // [[a,b],[c]]
```

### Guard / validation primitives

```csharp
// Replace with real examples from the package
Guard.AgainstNull(value, nameof(value));
Guard.AgainstNullOrEmpty(name, nameof(name));
```

---

## Compatibility

| Package version | .NET | Notes |
|----------------|------|-------|
| 2.x | .NET 10+ | Current. Managed via Nerdbank.GitVersioning in mono-repo |
| 1.6.x | .NET 10 | Final release from standalone repo. Security fixes only. |
| 1.5.x and earlier | .NET 8, 9 | End of life |

This package targets `net10.0` and has no platform-specific dependencies.
It runs on Windows, macOS, and Linux.

### Dependencies

This package has no external NuGet dependencies by design. It relies only
on the .NET 10 base class library.

---

## Changelog

### 1.6.3-pre — _in development_

- Migrated to AStar Development mono-repo
- Versioning now managed by Nerdbank.GitVersioning
- No API changes from 1.6.2

### 1.6.2 — _yyyy-mm-dd_

- Last release from standalone repository
- [Release notes](https://github.com/your-org/your-repo/releases/tag/v1.6.2)

[Full changelog →](https://github.com/your-org/your-repo/releases?q=AStar.Dev.Utilities)

---

## Contributing

This package lives in the [AStar Development mono-repo](https://github.com/your-org/your-repo)
under `packages/core/AStar.Dev.Utilities/`.

Bug reports and feature requests → [GitHub Issues](https://github.com/your-org/your-repo/issues)

To contribute a fix or feature:

1. Clone the repo and create a feature branch (`feat/utilities-my-fix`)
2. Make changes under `packages/core/AStar.Dev.Utilities/`
3. Add or update tests in `packages/core/AStar.Dev.Utilities.Tests.Unit/`
4. Run `dotnet test packages/core/AStar.Dev.Utilities.Tests.Unit` to verify
5. Open a pull request against `main`

See the [repo README](../../README.md) for full build and test instructions.

---

_Part of the [AStar Development](https://github.com/your-org) package family._
