# AStarDev.CloudSync.TestsUnit

Unit tests for AStarDev.CloudSync: view model tests, headless view tests and PNG snapshot tests.

## Snapshot tests

`Snapshots/Snapshot.cs` renders a window headlessly with Skia, at a fixed size (1280×800 by default) in a given theme, and compares it with a committed baseline PNG in `Snapshots/Baselines/`.

```csharp
Assert.SkipUnless(OperatingSystem.IsLinux(), Snapshot.LinuxOnlyReason);

Snapshot.ShouldMatchBaseline(window, ThemeVariant.Dark, "main-window-dark");
```

- A pixel differs when any channel differs by more than 8. A snapshot fails when more than 0.1% of its pixels differ.
- On failure, `artifacts/snapshots/<name>.actual.png` and `<name>.diff.png` are written. The diff shows changed pixels in red over a dimmed copy of the actual image. The failure message gives the full paths.
- Rendering is pinned for repeatable results: Inter font (`TestApp`), `en-GB` culture, and a fresh window per snapshot.

### Linux only

Font rasterisation differs between operating systems, so baselines are rendered on Linux and the tests skip elsewhere. CI (`dotnet-ci.yml`) runs on `ubuntu-latest` under `xvfb-run`.

### Updating baselines

Baselines change only when you ask for it:

```bash
cd apps/desktop/AStarDev.CloudSync/AStarDev.CloudSync.TestsUnit
UPDATE_SNAPSHOTS=1 dotnet test --no-build
dotnet test --no-build
```

1. Look at every changed PNG (`git diff --stat -- Snapshots/Baselines`) before committing it.
2. Commit baseline changes in the same commit as the UI change that caused them.
3. Add a row to the change log below.

### Baseline change log

This records how often baselines change during phase 1 (#1261), to judge the maintenance cost of snapshot testing.

| PR | Baselines changed | Reason |
|---|---|---|
| #1255 | 3 added | Harness introduced: empty main window (dark, light) and probe |
| #1256 | 14 added | Shared controls (7) in dark and light; existing 3 unchanged |
| #1258 | 2 added | Layout B (grouped by provider) in dark and light; existing 17 unchanged |
| #1259 | 2 added | Layout C (sync pairs first) in dark and light; existing 19 unchanged |
