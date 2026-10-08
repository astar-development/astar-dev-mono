# How to Publish

Independent tag-triggered release pipelines live in this repo. Each owns its own tag
namespace so pushing one tag only ever fires one workflow. Pick the right format below.

| What                           | Tag format                     | Workflow                                             |
| ------------------------------ | ------------------------------ | ---------------------------------------------------- |
| A NuGet package                | `{PackageName}/v{version}`     | `.github/workflows/nuget-publish.yml`                |
| A desktop app                  | `{app}-v{version}` (created automatically) | `.github/workflows/desktop-release-on-merge.yml` |

**Never reuse another row's tag format.** The patterns are deliberately disjoint
(slash-delimited vs. bare `v` vs. `scraper-v` vs. `file-app-v`) — mixing them up either
fires the wrong pipeline or fires two at once against the same GitHub Release.

---

## 1. Publish a NuGet package

Tag format: `{PackageName}/v{version}` — the tag name IS the version, nothing else to edit.

```bash
git tag AStarDev.Utilities/v0.1.0
git push origin AStarDev.Utilities/v0.1.0
```

Examples of valid package names (must match an existing `.csproj` under `packages/`,
any nesting depth):

```
AStarDev.Utilities/v1.6.8
AStar.Dev.Infrastructure.AppDb/v0.3.0
AStarDev.SourceGeneratorAttributes/v1.0.0
AStar.Dev.SomePackage/v2.1.0-beta.1     # prerelease: hyphen suffix
```

What happens: `nuget-publish.yml` extracts package name + version from the tag, locates
`packages/**/{PackageName}.csproj`, restores/builds/tests (if a matching
`{PackageName}.TestsUnit` project exists), packs, then pushes to GitHub Packages and
NuGet.org, and creates a GitHub Release with the `.nupkg`/`.snupkg` attached.

Fails fast if no `.csproj` matches the tagged package name — check the name is exact
(case-sensitive) before pushing.

---

## 2. Publish a desktop app

Desktop apps (Clock, File App, OneDrive Sync Client, Wallpaper Scraper) release
automatically. Nothing needs tagging by hand.

On every merge to `main`, `desktop-release-on-merge.yml`:

1. Works out which apps the merge touched, using the directories listed per app in
   `.github/desktop-apps.json`. Changes under `packages/` do not release an app.
2. Computes each touched app's next version from its latest stable `{tag-prefix}X.Y.Z`
   tag and the merged commit messages (`feat` = minor, `fix`/other = patch, `!` or a
   `BREAKING CHANGE:` footer = major; an app with no tag yet starts at `0.1.0`).
3. Pushes the tag and calls `desktop-app-release.yml`, which builds, tests, and publishes
   self-contained Velopack packages. `release-linux` runs first and is the only job that
   can fail the workflow; `release-other-platforms` (win-x64, osx-arm64) only starts after
   Linux succeeds and is best-effort (`continue-on-error: true`). All platforms publish to
   the **same** GitHub Release (`vpk upload --merge`).

| App                  | Tag prefix            |
| -------------------- | --------------------- |
| Clock                | `clock-v`             |
| File App             | `file-app-v`          |
| OneDrive Sync Client | `onedrive-sync-v`     |
| Wallpaper Scraper    | `wallpaper-scraper-v` |

To release one app manually (for example a pre-release), run the **Desktop release on
merge** workflow from the Actions tab (`workflow_dispatch`), choose the app and enter the
version, e.g. `0.35.0-rc.1`. Pushing a desktop tag by hand no longer triggers anything.

To add an app, add an entry to `.github/desktop-apps.json` and to the `app` options of the
`workflow_dispatch` input.

---

## Sanity checks before tagging

- Confirm you're tagging the intended commit: `git log -1 --oneline`
- Confirm no tag with that exact name already exists: `git tag -l "<tag>"`
- Push the tag, then watch the run: `gh run list --workflow=<workflow-file> --limit 1`
