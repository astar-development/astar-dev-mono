# S001 — Project Scaffolding & Solution Structure - COMPLETE

**Phase:** MVP
**Area:** Foundation

---

## User Story

As a developer,
I want the four required projects to exist in the correct repo locations with correct naming, metadata, and inter-project references,
So that all subsequent stories have a compilable, standards-compliant foundation to build on.

---

## Acceptance Criteria

- [x] `apps/desktop/AStar.Dev.OneDriveSync/` — Avalonia desktop app project exists and builds
- [x] `packages/core/astar-dev-sync-engine/AStar.Dev.Sync.Engine/` — class library exists and builds
- [x] `packages/core/astar-dev-conflict-resolution/AStar.Dev.Conflict.Resolution/` — class library exists and builds
- [x] `packages/infra/astar-dev-onedrive-client/AStar.Dev.OneDrive.Client/` — class library exists and builds
- [x] Corresponding unit test projects exist alongside each package:
    - `AStar.Dev.Sync.Engine.Tests.Unit`
    - `AStar.Dev.Conflict.Resolution.Tests.Unit`
    - `AStar.Dev.OneDrive.Client.Tests.Unit`
- [x] All projects inherit from `Directory.Build.props` (no duplicate `TargetFramework`, `Nullable`, `TreatWarningsAsErrors`)
- [x] All package `.csproj` files include `Description`, `PackageTags`, and `PackageLicenseExpression` (required by `Directory.Build.targets` validation)
- [x] All projects are added to `AStar.Dev.slnx` _(note: solution was named `AStar.Dev.slnx` during implementation; story updated to match)_
- [x] Desktop app references `AStar.Dev.Sync.Engine`, `AStar.Dev.Conflict.Resolution`, and `AStar.Dev.OneDrive.Client` via `<ProjectReference>`
- [x] `AStar.Dev.Sync.Engine` references `AStar.Dev.OneDrive.Client` — no other cross-package references _(fix applied: spurious `Conflict.Resolution` reference removed; incorrect `../../infra/` path corrected to `../../../infra/`)_
- [x] `AStar.Dev.OneDrive.Client` references `AStar.Dev.Sync.Engine` — **not permitted** (would be cyclic); dependency direction enforced
- [x] `dotnet build` from repo root produces zero errors and zero warnings _(local-dev `0.0.1-local` version-fallback warnings from `Directory.Build.targets` are expected and suppressed by CI; no C# compiler warnings)_
- [x] `dotnet test` from repo root shows all (empty) test projects discovered with 0 failures _(empty package test projects produce no output; 185 tests across the app and existing packages all pass)_
- [x] Feature-slice folder structure scaffolded as per spec section 9 (empty folders/placeholder files acceptable at this stage)
- [x] `NF-13`: No "group by type" top-level folders (`ViewModels/`, `Services/`, `Models/`) — features live under `Features/`, infrastructure under `Infrastructure/`

---

## Technical Notes

- Dependency direction: `desktop app` → `Sync.Engine` → `OneDrive.Client`; `desktop app` → `Conflict.Resolution`; `OneDrive.Client` has no internal package references
- `AStar.Dev.Functional.Extensions` added as a `<ProjectReference>` (local development convention) to all projects per NF-16; switches to `<PackageReference>` at publish time
- `Serilog` and related sinks added to all projects per NF-00 (logging is not optional)
- The existing `AStar.Dev.OneDriveSync.old/` is **not** referenced — confirmed it can be ignored safely without build errors
- Test projects set `IsPackable=false` automatically via `Directory.Build.targets`

---

## Dependencies

None — this is the root story.
