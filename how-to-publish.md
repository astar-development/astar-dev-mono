# How to Publish

Independent tag-triggered release pipelines live in this repo. Each owns its own tag
namespace so pushing one tag only ever fires one workflow. Pick the right format below.

| What                           | Tag format                 | Workflow                                             |
| ------------------------------ | -------------------------- | ---------------------------------------------------- |
| A NuGet package                | `{PackageName}/v{version}` | `.github/workflows/nuget-publish.yml`                |
| OneDrive Sync Client (desktop) | `onedrive-sync-v{version}` | `.github/workflows/onedrive-sync-client-release.yml` |
| Wallpaper Scraper (desktop)    | `scraper-v{version}`       | `.github/workflows/scraper-release.yml`              |
| File App (desktop)             | `file-app-v{version}`      | `.github/workflows/file-app-release.yml`             |
| Clock (desktop)                | `clock-v{version}`         | `.github/workflows/clock-release.yml`                |

**Never reuse another row's tag format.** The patterns are deliberately disjoint
(slash-delimited vs. bare `v` vs. `scraper-v` vs. `file-app-v`) — mixing them up either
fires the wrong pipeline or fires two at once against the same GitHub Release.

---

## 1. Publish a NuGet package

Tag format: `{PackageName}/v{version}` — the tag name IS the version, nothing else to edit.

```bash
git tag AStar.Dev.Utilities/v1.7.0
git push origin AStar.Dev.Utilities/v1.7.0
```

Examples of valid package names (must match an existing `.csproj` under `packages/`,
any nesting depth):

```
AStar.Dev.Utilities/v1.6.8
AStar.Dev.Infrastructure.AppDb/v0.3.0
AStar.Dev.Source.Generators.Attributes/v1.0.0
AStar.Dev.SomePackage/v2.1.0-beta.1     # prerelease: hyphen suffix
```

What happens: `nuget-publish.yml` extracts package name + version from the tag, locates
`packages/**/{PackageName}.csproj`, restores/builds/tests (if a matching
`{PackageName}.Tests.Unit` project exists), packs, then pushes to GitHub Packages and
NuGet.org, and creates a GitHub Release with the `.nupkg`/`.snupkg` attached.

Fails fast if no `.csproj` matches the tagged package name — check the name is exact
(case-sensitive) before pushing.

---

## 2. Publish the OneDrive Sync Client

Tag format: bare `v{version}`.

```bash
git tag onedrive-sync-v0.36.2
git push origin onedrive-sync-v0.36.2
```

Prerelease: `git tag onedrive-sync-v0.35.0-rc.1`

What happens: `onedrive-sync-client-release.yml` builds, tests, and publishes
self-contained Velopack packages. `release-linux` runs first and is the only job that can
fail the workflow (Linux is this project's primary platform); `release-other-platforms`
(win-x64, osx-arm64) only starts after Linux succeeds and is best-effort
(`continue-on-error: true`) — a Windows/macOS packaging problem never blocks the Linux
release. All platforms publish to the **same** GitHub Release (`vpk upload --merge`).

---

## 3. Publish the Wallpaper Scraper

Tag format: `scraper-v{version}` — **not** bare `v{version}` (that's the OneDrive
namespace — see the collision note below).

```bash
git tag scraper-v0.10.13
git push origin scraper-v0.10.13
```

What happens: `scraper-release.yml` publishes self-contained linux-x64 and win-x64
builds, packs each with `vpk`, and uploads both to the same GitHub Release
(`--merge`, jobs serialized via `max-parallel: 1` to avoid a race creating the release
twice).

---

## 4. Publish the File App

Tag format: `file-app-v{version}`.

```bash
git tag file-app-v0.1.2
git push origin file-app-v0.1.2
```

Prerelease: `git tag file-app-v0.1.0-rc.1`

What happens: `file-app-release.yml` builds, tests, and publishes self-contained Velopack
packages, mirroring the OneDrive Sync Client's workflow shape — `release-linux` runs
first and is the only job that can fail the workflow; `release-other-platforms` (win-x64,
osx-arm64) only starts after Linux succeeds and is best-effort (`continue-on-error: true`).
All platforms publish to the **same** GitHub Release (`vpk upload --merge`).

---

## 5. Publish the Clock

Tag format: `clock-v{version}`.

```bash
git tag clock-v0.1.1
git push origin clock-v0.1.1
```

Prerelease: `git tag clock-v0.1.0-rc.1`

What happens: `clock-release.yml` builds, tests, and publishes self-contained Velopack
packages, mirroring the OneDrive Sync Client's workflow shape — `release-linux` runs
first and is the only job that can fail the workflow; `release-other-platforms` (win-x64,
osx-arm64) only starts after Linux succeeds and is best-effort (`continue-on-error: true`).
All platforms publish to the **same** GitHub Release (`vpk upload --merge`).

---

## Sanity checks before tagging

- Confirm you're tagging the intended commit: `git log -1 --oneline`
- Confirm no tag with that exact name already exists: `git tag -l "<tag>"`
- Push the tag, then watch the run: `gh run list --workflow=<workflow-file> --limit 1`
