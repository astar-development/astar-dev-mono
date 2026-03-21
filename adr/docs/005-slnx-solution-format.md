# 005 — Use SLNX solution format

**Date**: 2026-03-20
**Status**: Accepted

---

## Context

Visual Studio and the .NET CLI have historically used the `.sln` file format
to group projects into a solution. The format is decades old, text-based but
not human-friendly, and has well-known pain points in a mono-repo context:

- Merge conflicts are common when multiple branches add or remove projects
  simultaneously — the format's GUID-heavy structure makes conflicts hard to
  resolve manually
- The file is verbose: a simple project reference requires four lines including
  two GUIDs that carry no semantic meaning
- Nested solution folders (used to mirror the repo's directory structure in the
  IDE) require additional GUID entries and are fragile under rename operations
- The format is not designed to be read or edited by hand, yet it lives in
  source control and appears in every diff that touches the solution structure

Microsoft introduced the SLNX format (`.slnx`) in Visual Studio 2022 17.10 and
the .NET SDK as a cleaner, XML-based replacement. As of .NET 10 it is fully
supported by the SDK and JetBrains Rider.

---

## Decision

Use the SLNX format (`.slnx`) for the mono-repo solution file (`MonoRepo.slnx`).

The SLNX format is:

- Human-readable XML with no GUIDs
- Significantly more compact — a project reference is a single line
- Merge-conflict-friendly — XML diffs are cleaner and conflicts are easier
  to resolve
- Fully supported by `dotnet build`, `dotnet test`, `dotnet restore`, Rider,
  and the C# Dev Kit in VS Code as of their current versions at the time of
  this decision

The `.vscode/settings.json` file points the C# Dev Kit at `MonoRepo.slnx` via
`"dotnet.defaultSolution": "MonoRepo.slnx"`.

---

## Consequences

**Becomes easier:**

- Adding or removing a project from the solution is a clean, readable one-line
  diff — no GUIDs, no four-line blocks, no mysterious type identifiers
- Merge conflicts on the solution file are rare and, when they occur, trivially
  resolved by reading the XML
- The solution file can be sensibly code-reviewed — a PR that adds a new
  package project shows exactly one new `<Project>` line in the solution diff
- Solution folders map directly to the filesystem structure without the
  additional GUID overhead of the legacy format

**Becomes harder:**

- Tooling that does not yet support SLNX cannot open the solution. At the time
  of this decision this is not a concern — all tooling in active use (Rider,
  VS Code C# Dev Kit, .NET 10 CLI) supports SLNX fully
- If a contractor or external contributor uses an older IDE that does not
  support SLNX, they cannot open the solution file directly. Individual project
  files remain fully usable — only the solution grouping is unavailable

**Risks and mitigations:**

- _Risk_: a future tooling update regresses SLNX support, breaking the
  developer experience.
  _Mitigation_: the `.slnx` format is a first-party Microsoft format with SDK
  support. Regression risk is low. If it occurs, conversion back to `.sln`
  is a non-destructive operation — no project files are affected.

---

## Alternatives considered

### Classic `.sln` format

The legacy format works everywhere and has no compatibility risk. However, given
that all tooling in use fully supports SLNX and the format improvements are
meaningful in a large mono-repo (readability, merge conflict reduction), the
classic format offers no advantage that justifies its drawbacks at this point.

### No solution file

The .NET CLI does not require a solution file — `dotnet build` and `dotnet test`
can target individual projects or use the `Directory.Build.props` cascade
without one. However, Rider and VS Code's C# Dev Kit both use the solution file
to populate the project explorer, provide cross-project navigation, and scope
IntelliSense. Omitting the solution file significantly degrades the IDE
experience in a multi-project repository.

### Multiple solution files

A solution file per sub-folder (`packages/Packages.slnx`,
`apps/Apps.slnx`) would allow developers to open a subset of the repo. This
adds maintenance overhead — each new project must be added to the right solution
file — without a meaningful benefit for a one-person team where opening the
full solution is not a performance problem. Filtered solution files (`.slnf`)
are available as a lighter-weight mechanism for subsetting when needed,
without requiring a separate solution file to maintain.
