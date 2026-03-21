# Documentation

Architecture decisions, guides, and reference material for the AStar Development
mono-repo. This folder is the long-form companion to the [repo README](../README.md)
— the README tells you how to use the repo day-to-day; this folder explains the
decisions behind it.

---

## Contents

- [Architecture decisions](#architecture-decisions)
- [Guides](#guides)
- [Reference](#reference)

---

## Architecture decisions

Architecture Decision Records (ADRs) document significant choices made in this
repo — what was decided, why, and what alternatives were considered. They are
written once and rarely updated; if a decision is reversed, a new ADR is written
rather than editing the old one.

| #                                             | Title                                                    | Status   | Date         |
| --------------------------------------------- | -------------------------------------------------------- | -------- | ------------ |
| [001](adr/001-mono-repo.md)                   | Adopt a mono-repo structure                              | Accepted | _yyyy-mm-dd_ |
| [002](adr/002-central-package-management.md)  | Use Central Package Management                           | Accepted | _yyyy-mm-dd_ |
| [003](adr/003-avalonia-for-desktop.md)        | Avalonia for cross-platform desktop                      | Accepted | _yyyy-mm-dd_ |
| [004](adr/004-github-packages-for-nuget.md)   | GitHub Packages as NuGet feed                            | Accepted | _yyyy-mm-dd_ |
| [005](adr/005-slnx-solution-format.md)        | Use SLNX solution format                                 | Accepted | _yyyy-mm-dd_ |
| [006](adr/006-nuget-configuration-lessons.md) | NuGet configuration quirks resolved during initial setup | Accepted | 2026-03-21   |

### Adding a new ADR

Copy `adr/000-template.md`, increment the number, and fill in the sections.
Keep ADRs short — one page is the target. The goal is to capture the reasoning,
not write an essay.

---

## Guides

Step-by-step instructions for common tasks that go beyond what fits in the
repo README.

| Guide                                                            | Description                                                                                             |
| ---------------------------------------------------------------- | ------------------------------------------------------------------------------------------------------- |
| [Adding a new NuGet package](guides/adding-a-new-package.md)     | Scaffold a new package project, wire it into the solution, and verify it builds and publishes correctly |
| [Adding a new Blazor app](guides/adding-a-new-blazor-app.md)     | Scaffold a new Blazor application, configure it for the mono-repo, and set up its CI path filter        |
| [Adding a new Avalonia app](guides/adding-a-new-avalonia-app.md) | Scaffold a new Avalonia desktop app and configure cross-platform publish targets                        |
| [Adding a new Next.js app](guides/adding-a-new-nextjs-app.md)    | Scaffold a Next.js app inside `apps/web/`, configure shared ESLint/Prettier, and integrate with CI      |
| [Releasing a new version](guides/releasing-a-new-version.md)     | End-to-end release process from tagging to published packages and GitHub Release                        |
| [Working with Terraform](guides/working-with-terraform.md)       | Local Terraform workflow, state management, and the staging → prod promotion process                    |
| [Updating the .NET SDK](guides/updating-dotnet-sdk.md)           | How to bump `global.json`, update package versions, and validate the upgrade                            |
| [Troubleshooting CI](guides/troubleshooting-ci.md)               | Common CI failures and how to diagnose them                                                             |

---

## Reference

Background material and standards that apply across the repo.

| Document                                                  | Description                                                                                            |
| --------------------------------------------------------- | ------------------------------------------------------------------------------------------------------ |
| [Package naming conventions](reference/package-naming.md) | The `AStar.Dev.[Area].[Name]` naming scheme and area definitions                                       |
| [Versioning policy](reference/versioning-policy.md)       | How semantic versioning is applied, what constitutes a breaking change, and the pre-release convention |
| [Branch and PR strategy](reference/branching-strategy.md) | Branch naming, merge strategy, and PR requirements                                                     |
| [NuGet feed access](reference/nuget-feed-access.md)       | How to grant a contractor or CI system access to GitHub Packages                                       |
| [Secrets management](reference/secrets-management.md)     | Where secrets live, how they're rotated, and what never goes in the repo                               |

---

## ADR template

For reference — the structure used for every ADR in this repo:

```markdown
# NNN — Title

**Date**: yyyy-mm-dd
**Status**: Proposed | Accepted | Deprecated | Superseded by [NNN](NNN-title.md)

## Context

What situation prompted this decision? What constraints existed?

## Decision

What was decided? Be specific.

## Consequences

What becomes easier? What becomes harder?
What follow-up actions or risks does this introduce?

## Alternatives considered

What else was evaluated and why was it not chosen?
```
