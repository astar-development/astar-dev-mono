# 001 — Adopt a mono-repo structure

**Date**: 2026-03-20
**Status**: Accepted

---

## Context

AStar Development maintains a growing portfolio of related products and libraries:

- 5–6 web applications (Blazor and Next.js)
- 3–4 cross-platform desktop applications (Avalonia)
- ~50 NuGet packages published for external consumption
- Shared infrastructure-as-code (Terraform) and automation scripts

Historically these lived in separate repositories. As the portfolio grew, this
created compounding friction:

- **Dependency lag**: updating a shared package required publishing it, then
  updating the version reference in every consuming repo separately — often
  across 5–10 repos per change
- **CI duplication**: every repo maintained its own GitHub Actions workflows,
  NuGet.Config, `.editorconfig`, and MSBuild config — 50+ places to apply the
  same security fix or tooling update
- **Context switching**: working on a feature that spanned a package and its
  consuming app required juggling multiple clones, branches, and IDE windows
- **Inconsistency drift**: formatting rules, analyser settings, and SDK
  versions had quietly diverged across repos over time with no mechanism to
  detect or correct this
- **Dependency graph opacity**: there was no single view of what consumed what,
  making it difficult to assess the impact of a breaking change before making it

The team size is one person. The overhead of managing 60+ repositories is not
justified by any benefit that repository isolation provides at this scale.

---

## Decision

Consolidate all AStar Development products, packages, scripts, and
infrastructure into a single Git repository with the following top-level
structure:

```
/
├── apps/
│   ├── web/        — Blazor and Next.js applications
│   └── desktop/    — Avalonia desktop applications
├── packages/
│   ├── core/       — Domain models and business logic
│   ├── infra/      — Data access, auth, logging, HTTP
│   └── ui/         — Shared UI components
├── infra/          — Terraform (staging and prod environments)
├── scripts/        — Build and release automation
├── tests/          — Repo-wide integration and E2E tests
└── docs/           — Architecture decisions and guides
```

Shared configuration is expressed once at the repo root and cascades to all
projects via MSBuild's `Directory.Build.props` / `Directory.Build.targets`
inheritance, Central Package Management (`Directory.Packages.props`), a pinned
SDK version (`global.json`), and a single `.editorconfig`.

CI is implemented as three path-filtered GitHub Actions workflows rather than
one monolithic workflow, so only the affected subset of the repo builds on any
given push.

---

## Consequences

**Becomes easier:**

- Cross-cutting changes (SDK bump, new analyser rule, workflow update) are a
  single commit affecting the whole codebase immediately
- A change to a shared package is immediately visible to all consuming apps
  within the repo via `<ProjectReference>` — no publish/update cycle during
  development
- The full dependency graph is visible at a glance; impact analysis for
  breaking changes is trivial
- One clone, one IDE window, one set of run configurations covers all active
  work
- Atomic commits across package and consumer — a breaking change and all its
  call-site fixes land in one commit with a coherent message
- Formatting, analyser settings, and SDK version are structurally enforced
  rather than maintained by convention

**Becomes harder:**

- Repository size will grow over time as history accumulates across all
  products; `git clone` and some git operations will be slower than in small
  focused repos (mitigated by shallow clones for CI)
- A single misconfigured root file (e.g. `Directory.Build.props`) can break
  every project simultaneously; changes to root config require extra care and
  should always be validated with a full `dotnet build` before committing
- External contributors to a specific package see the entire codebase rather
  than just the package; this is acceptable given there are currently no
  external contributors and the repo is private

**Risks and mitigations:**

- _Risk_: path filters in CI workflows are accidentally incomplete, causing
  changes to be missed by CI.
  _Mitigation_: root-level MSBuild config files (`Directory.Build.props`,
  `Directory.Packages.props`, `global.json`) are explicitly included in every
  workflow's path filter — a change to any of them triggers all workflows.

- _Risk_: a single version tag applies to all packages simultaneously, which
  may feel coarse if packages have very different rates of change.
  _Mitigation_: accepted as a deliberate simplification for a one-person team.
  Per-package tagging (strategy B in `nuget-publish.yml`) is documented as a
  future migration path if independent release cadences become necessary.

---

## Alternatives considered

### Keep separate repositories, improve tooling

Using a tool like Renovate Bot to automate cross-repo dependency updates would
reduce the version-bump friction. However it does not address context switching,
CI duplication, inconsistency drift, or the lack of atomic cross-repo commits.
The overhead remains proportional to the number of repos regardless of tooling.

### Partial consolidation (packages only)

Consolidating only the ~50 NuGet packages into a single repo while keeping apps
separate would solve the package maintenance problem but not the app/package
development friction. The boundary between "package" and "app" is also
artificial — Blazor components that are shared across apps blur this line.
Partial consolidation creates a new coordination problem (which repo owns the
shared Blazor components?) without solving the original ones.

### Nx or Turborepo as a build orchestrator

Nx supports .NET projects and would provide dependency-graph-aware incremental
builds, potentially faster than path-filtered GitHub Actions. However it
introduces a significant new tool with its own learning curve, configuration
surface, and failure modes. MSBuild's native incremental build combined with
path-filtered workflows is sufficient for this portfolio size and avoids the
additional dependency. This decision can be revisited if build times become a
meaningful problem.
