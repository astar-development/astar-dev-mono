# 002 — Use Central Package Management

**Date**: 2026-03-20
**Status**: Accepted

---

## Context

The mono-repo contains ~50 NuGet packages and several applications, all of which
share a significant number of common dependencies (Microsoft.Extensions.*,
Serilog, Entity Framework Core, xUnit, etc.).

In the traditional per-project model, each `.csproj` declares its own
`<PackageReference>` entries including an explicit version number. Across 50+
projects this creates several problems:

- **Version skew**: different projects silently pull different versions of the
  same package. This is particularly dangerous for packages that must be
  version-consistent across a dependency graph — EF Core being the canonical
  example, where mixing `10.0.0` and `10.0.1` across projects in the same
  process can cause runtime failures
- **Update overhead**: bumping a shared dependency requires editing every
  `.csproj` that references it — a tedious and error-prone manual process
  across 50+ projects
- **Diamond dependency confusion**: when package A depends on Serilog 4.1 and
  package B depends on Serilog 4.2, NuGet's resolution is non-obvious and
  the result varies by project; without a central authority there is no
  consistent answer
- **Review noise**: version numbers scattered across dozens of project files
  make pull request diffs harder to read — a dependency bump touches many
  files when it should touch one

.NET SDK 6+ ships Central Package Management (CPM) as a first-class feature
with no additional tooling required.

---

## Decision

Enable Central Package Management via `Directory.Packages.props` at the repo
root with `<ManagePackageVersionsCentrally>true</ManagePackageVersionsCentrally>`.

All NuGet package versions are declared once in `Directory.Packages.props`.
Individual `.csproj` files reference packages without version numbers:

```xml
<!-- Directory.Packages.props — one place -->
<PackageVersion Include="Serilog" Version="4.2.0" />

<!-- Any .csproj — no version -->
<PackageReference Include="Serilog" />
```

`<CentralPackageTransitivePinningEnabled>true</CentralPackageTransitivePinningEnabled>`
is also set, allowing transitive dependencies to be explicitly pinned from the
central file when needed.

Any `.csproj` that specifies a version without `VersionOverride` causes a build
error, making version drift structurally impossible.

---

## Consequences

**Becomes easier:**

- Bumping a dependency is a one-line change in one file — immediately visible
  in code review as a deliberate, intentional change
- Version consistency across all projects is guaranteed by the build system
  rather than by convention
- Diamond dependency resolution is explicit and deterministic — transitive
  pinning means the answer is always in `Directory.Packages.props`
- `dotnet outdated` run against the repo root audits all 50+ packages in one
  command

**Becomes harder:**

- A project that genuinely needs a different version of a package must use
  `VersionOverride`, which is slightly more verbose and stands out visually
  (intentionally — it should be rare and deliberate)
- Developers unfamiliar with CPM may be confused by `.csproj` files that
  contain `<PackageReference>` without a version; this is mitigated by the
  comment at the top of `Directory.Packages.props` explaining the pattern

**Risks and mitigations:**

- _Risk_: a package added to a `.csproj` without a corresponding entry in
  `Directory.Packages.props` causes a build error, potentially blocking work.
  _Mitigation_: the error message is clear and actionable; adding the entry
  to `Directory.Packages.props` is the obvious fix. This is a feature, not
  a bug — it prevents unreviewed versions entering the build.

---

## Alternatives considered

### Per-project versions with a shared props import

A `SharedPackageVersions.props` file imported by each `.csproj` could centralise
versions without CPM. This was common before CPM shipped. It requires explicit
opt-in in every project file, is not enforced by the build system, and is
superseded by CPM which achieves the same goal with first-class tooling support.

### Paket

Paket is a mature alternative dependency manager for .NET with strong mono-repo
support. It provides more powerful dependency resolution than NuGet and has
excellent lock file support. However it requires installing and maintaining an
additional tool outside the .NET SDK, and CPM now covers the primary use case
(centralised version management) without that overhead. Paket remains a valid
choice for repositories with very complex dependency graphs; this repo does not
currently meet that bar.
