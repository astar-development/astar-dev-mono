# 004 — GitHub Packages as NuGet feed

**Date**: 2026-03-20
**Status**: Accepted

---

## Context

~50 NuGet packages are published for external consumption. A hosting decision
is required: where do these packages live, and how do internal projects and
external consumers discover and download them?

The requirements are:

- Accessible to external consumers (public or authenticated access)
- Integrated with the existing GitHub-hosted source repository and GitHub
  Actions CI/CD
- Supports both `.nupkg` (package) and `.snupkg` (symbols) formats
- Reasonable cost for a one-person operation
- Source mapping support to prevent dependency confusion attacks (see ADR 002)

---

## Decision

Publish all NuGet packages to **GitHub Packages** as the primary feed, with
**NuGet.org** as an optional secondary feed for packages intended for the
broadest possible public audience.

The publish workflow (`nuget-publish.yml`) pushes to GitHub Packages using
`GITHUB_TOKEN` — no additional secrets required for the primary feed. A
commented-out step in the same workflow handles NuGet.org publication when
a `NUGET_ORG_API_KEY` secret is present.

`NuGet.Config` at the repo root configures Package Source Mapping so that all
`AStar.Dev.*` packages resolve exclusively from the GitHub Packages feed,
and all other packages resolve from NuGet.org. This prevents dependency
confusion attacks where a malicious package on NuGet.org shares a name with a
private package.

External consumers authenticate to GitHub Packages using a Personal Access
Token with `read:packages` scope. This is a one-time setup step documented in
the repo README and each package README.

---

## Consequences

**Becomes easier:**

- Package publication is fully automated via `GITHUB_TOKEN` — no external
  service credentials to rotate or manage for the primary feed
- Package pages on GitHub link directly to the source repository, commit, and
  release that produced them — full provenance out of the box
- Symbol packages (`.snupkg`) are supported natively, allowing consumers to
  step through package source in their debugger
- The GitHub Packages feed and the source repository are governed by the same
  access controls — access to the repo implies access to the packages

**Becomes harder:**

- External consumers must authenticate even for packages that could otherwise
  be public — GitHub Packages does not support fully anonymous access for
  packages in private repositories. Packages in public repositories can be
  downloaded without authentication, but authentication is still required for
  `dotnet restore` in many CI environments
- Consumers unfamiliar with GitHub Packages must complete a one-time PAT setup
  step that NuGet.org consumers do not face
- Discovery is lower than NuGet.org — packages hosted only on GitHub Packages
  do not appear in `nuget.org` search results

**Risks and mitigations:**

- _Risk_: GitHub Packages availability affects the ability to restore
  dependencies in CI and for external consumers.
  _Mitigation_: GitHub Packages shares GitHub's availability SLA. For packages
  where availability is critical, publishing to NuGet.org as well provides a
  redundant feed. The `NuGet.Config` source ordering can be adjusted to prefer
  NuGet.org for packages published to both.

- _Risk_: A consumer's PAT expires or is revoked, breaking their restore.
  _Mitigation_: This is documented as a known friction point in both the repo
  README and each package README, with clear instructions for renewal. It is
  an inherent tradeoff of authenticated feeds.

---

## Alternatives considered

### NuGet.org exclusively

NuGet.org is the standard public feed with the lowest friction for external
consumers — no authentication required for public packages. However it requires
managing a separate API key secret, has no integration with `GITHUB_TOKEN`, and
provides less provenance information linking a package version to its source
commit and CI run. For a portfolio where most consumers are known rather than
anonymous, the GitHub Packages authentication requirement is an acceptable
tradeoff for the tighter integration.

### Azure Artifacts

Azure Artifacts supports NuGet feeds with fine-grained access control and
integrates well with Azure DevOps pipelines. Given that CI is already on GitHub
Actions and source is on GitHub, adding Azure Artifacts would introduce a
third platform to manage. The integration overhead is not justified at this
scale.

### Self-hosted BaGet or Sleet

Running a self-hosted NuGet server (BaGet is a popular lightweight option)
provides maximum control over access, storage, and availability. However it
introduces infrastructure to operate and maintain — a server, storage, TLS
certificate, and uptime monitoring. For a one-person team this operational
overhead is not justified when managed options exist.
