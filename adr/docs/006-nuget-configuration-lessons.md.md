# 006 — NuGet configuration quirks resolved during initial mono-repo setup

**Date**: 2026-03-21
**Status**: Accepted

---

## Context

During the migration of the first package (`AStar.Dev.Utilities`) into the
mono-repo, several NuGet configuration issues were encountered that are not
well-documented and are likely to affect anyone setting up a similar .NET 10
mono-repo on Linux. This ADR documents the problems and their resolutions so
they are not rediscovered.

The environment in which these issues manifested:

- .NET SDK 10.0.104
- NuGet 7.0.2.0 (ships with the above SDK)
- Ubuntu Linux (development machine)
- GitHub Packages as the private NuGet feed
- Central Package Management enabled via `Directory.Packages.props`

---

## Issues encountered and resolved

### 1. `<clear />` self-closing tags rejected by NuGet parser

**Symptom:**

```
Unable to parse config file because: Missing required attribute 'key'
in element 'packageSource'. Path: '/path/to/NuGet.Config'.
```

**Cause:** NuGet 7.0.2.0's config parser rejects self-closing tags on its
own specific elements (`<clear />`, `<add />`, `<remove />`) in certain
positions, despite these being valid XML. The error message is misleading —
it doesn't mention self-closing tags at all.

**Resolution:** Replace all self-closing tags in `NuGet.Config` with explicit
open/close pairs:

```xml
<!-- Before -->
<clear />

<!-- After -->
<clear></clear>
```

---

### 2. `<packageSourceMapping>` requires `key` not `name`

**Symptom:** Same misleading error as above:

```
Unable to parse config file because: Missing required attribute 'key'
in element 'packageSource'.
```

**Cause:** The [NuGet documentation](https://aka.ms/nuget-package-source-mapping)
shows `name` as the attribute for `<packageSource>` elements inside
`<packageSourceMapping>`. NuGet 7.0.2.0 rejects `name` and requires `key`
instead — inconsistent with the published documentation.

**Resolution:** Use `key` throughout `<packageSourceMapping>`:

```xml
<!-- Before (matches docs but rejected by NuGet 7.0.2.0) -->
<packageSource name="github">
  <package pattern="AStar.Dev.*" />
</packageSource>

<!-- After (accepted by NuGet 7.0.2.0) -->
<packageSource key="github">
  <package pattern="AStar.Dev.*" />
</packageSource>
```

**Note:** This may be corrected in a future SDK/NuGet patch release. If
`<packageSourceMapping>` stops working after an SDK update, try reverting
`key` back to `name`.

---

### 3. `globalPackagesFolder` environment variable not expanded on Linux

**Symptom:** NuGet created literal folders inside the repo:

```
/path/to/repo/%USERPROFILE%/.nuget/packages/
/path/to/repo/~/.nuget/packages/
```

**Cause:** `%USERPROFILE%` is a Windows environment variable. On Linux it is
not set by default, so NuGet treated it as a literal string and created a
folder with that name inside the repo. The `~` tilde shorthand for `$HOME`
is a shell expansion, not an OS-level path feature — NuGet does not expand
it when reading `NuGet.Config`.

**Resolution:** Remove the `globalPackagesFolder` setting from `NuGet.Config`
entirely. Without it, NuGet uses its built-in platform-appropriate default:

- Windows: `%USERPROFILE%\.nuget\packages` (correctly resolved by Windows)
- Linux/macOS: `~/.nuget/packages` (correctly resolved by the runtime)

There is no cross-platform string that works in `NuGet.Config` for this
setting. Omitting it is the correct cross-platform approach.

The two rogue folders were removed and the following entries added to
`.gitignore` as a permanent safety net:

```
%USERPROFILE%/
~/
```

---

### 4. `packages/` in `.gitignore` silenced the entire source folder

**Symptom:** All files added to `packages/core/AStar.Dev.Utilities/` were
invisible to git — not shown as untracked, not staged, completely absent
from `git status`.

**Cause:** The repo's `.gitignore` contained a `packages/` entry, originally
intended to exclude the NuGet package restore folder (a convention from NuGet
2.x/3.x where packages were restored into a local `packages/` folder inside
the repo). This entry silently matched the `packages/` source folder we
created for package source code.

**Resolution:** Removed the `packages/` entry from `.gitignore`. The NuGet
global package cache is stored outside the repo by default
(`~/.nuget/packages`) and does not need to be gitignored. The entry was
serving no purpose and causing significant harm.

**Lesson:** When setting up a mono-repo with a `packages/` source folder,
audit `.gitignore` for any broad directory exclusions that may have been
inherited from project templates or older conventions.

---

### 5. `Directory.Build.props` case sensitivity on Linux

**Symptom:** The `` value was empty, causing:

```
The TargetFramework value '' was not recognized.
```

**Cause:** The file had been saved as `Directory.build.props` (lowercase `b`)
rather than `Directory.Build.props`. On Windows the NTFS filesystem is
case-insensitive and MSBuild found the file regardless. On Linux the ext4
filesystem is case-sensitive and MSBuild did not find the file, so the
`Directory.Build.props` cascade was silently broken.

**Resolution:** Renamed the file to the correct casing:
`Directory.Build.props`, `Directory.Build.targets`, `Directory.Packages.props`.

**Lesson:** All MSBuild convention filenames are PascalCase. On a
cross-platform repo developed primarily on Linux, case errors in these
filenames will fail silently in the most confusing way possible — the build
appears to work but inherits none of the shared configuration.

---

### 6. Nerdbank.GitVersioning requires committed `version.json`

**Symptom:**

```
rev-parse produced no commit for packages/core/AStar.Dev.Utilities
```

**Cause:** Nerdbank.GitVersioning calculates the version height by counting
commits since `version.json` was introduced into git history. Running
`nbgv get-version` before committing `version.json` gives it no history
to walk.

**Resolution:** Always commit `version.json` before running any build or
`nbgv` command. The correct sequence is:

1. Create `version.json`
2. `git add version.json && git commit`
3. `nbgv get-version -p <project-path>`
4. `dotnet build`

**Lesson:** nbgv's version is a function of git history, not just file
content. No commit = no version = confusing errors downstream.

---

### 7. `nbgv get-version` path argument is not a project path

**Symptom:**

```
rev-parse produced no commit for packages/core/AStar.Dev.Utilities
```

(even after committing `version.json`)

**Cause:** `nbgv get-version packages/core/AStar.Dev.Utilities` interprets
the argument as a git commit-ish reference, not a filesystem path to a
project directory.

**Resolution:** Use the `-p` / `--project` flag to specify a project directory:

```bash
nbgv get-version -p packages/core/AStar.Dev.Utilities
```

Or `cd` into the project directory first:

```bash
cd packages/core/AStar.Dev.Utilities && nbgv get-version
```

---

## Consequences

All seven issues are resolved in the current repo configuration. The
resolutions are reflected in:

- `NuGet.Config` — `<clear></clear>`, `key` attribute in mapping, no
  `globalPackagesFolder`
- `.gitignore` — `packages/` entry removed, rogue folder patterns added
- `Directory.Build.props` / `Directory.Build.targets` / `Directory.Packages.props`
  — correct PascalCase filenames on disk
- `version.json` — committed before first build
- Developer documentation — this ADR and the migration checklist updated

Any developer setting up a new development machine or migrating the next
package should read this ADR before starting to avoid repeating the same
diagnostic sessions.
