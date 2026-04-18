# Fix: Pre-load Full OneDrive Folder Tree

## Problem

Multiple attempts (#162–#165, 0600aeb) to fix nested folder sync have failed. When a root folder (e.g. "Documents") is selected, the app should recursively download all descendants unless the user explicitly excludes them. Sub-folders are silently ignored.

## Root Cause (Compound)

### 1 — Lazy-loading breaks `CollectSyncDecisions`

`FolderTreeNodeViewModel` lazy-loads children only when the user expands a node in the UI. `CollectSyncDecisions` (called on every toggle) walks `node.Children`. For nodes the user never expanded, `Children` is empty, so no sub-folder DB rows are written. On next boot, `ExplicitlyExcludedFolderIds` is incomplete, and the sync engine either includes folders it shouldn't or misses ones it should sync.

### 2 — Dual-pass initial enumeration is fragile

`FullEnumerationAsync` does:

1. `EnumerateSubFolderAsync` — custom recursive children-API walk, collects items
2. `ConsumeDeltaToGetLinkAsync` — second delta sweep over the same tree, discards items, captures link

Two separate passes over potentially large folder trees, with a race window between them. This approach has failed across multiple fix iterations.

### 3 — Initial and incremental paths diverge

Initial sync (full enumeration) uses a custom tree walk with manual path-building. Incremental sync (delta link) uses `MapToDeltaItem` which builds paths from `parentReference.Path`. Any mismatch between these two path-computation strategies causes incorrect relative paths, which breaks local-path resolution in `BuildJobs`.

## Proposed Fix

### A — Pre-load the full folder tree

Add `GetAllFoldersAsync` to `IGraphService`. In `AccountFilesViewModel.LoadAsync`, call it after auth and build the complete `FolderTreeNodeViewModel` hierarchy upfront. No lazy loading. Children are fully populated before `CollectSyncDecisions` is ever invoked. `HasChildren` is accurate from real data.

### B — Unify initial and incremental sync paths

Replace `FullEnumerationAsync` (dual-pass) with a single delta sweep:

- Call `GetAsDeltaGetResponseAsync` without a prior link
- Page through all responses collecting items via `MapToDeltaItem` (same method as incremental)
- Capture `OdataDeltaLink` from the terminal page
- Apply `DeltaItemExclusionFilter.Filter` (same as incremental)

Eliminates `EnumerateSubFolderAsync`, `ConsumeDeltaToGetLinkAsync`, and `ResolveEffectiveFolderPathAsync`. Also removes the `folderRelativePath` parameter from `GetDeltaAsync` (was only needed for manual path-building, which `MapToDeltaItem` already handles via `parentReference.Path`).

## Changes Required

### `IGraphService` / `GraphService`

- Add `GetAllFoldersAsync(accessToken, ct)` → flat `List<DriveFolder>` via recursive children API (select `id,name,folder,parentReference`)
- Remove `folderRelativePath` param from `GetDeltaAsync`
- Replace `FullEnumerationAsync` body with single delta sweep
- Delete `EnumerateSubFolderAsync`, `ConsumeDeltaToGetLinkAsync`, `ResolveEffectiveFolderPathAsync`

### `FolderTreeNodeViewModel`

- Remove `IGraphService`, `accessToken`, `driveId` from constructor
- Remove `EnsureChildrenLoadedAsync`, `_childrenLoaded`, `IsLoadingChildren` observable
- `ToggleExpandAsync` → `ToggleExpand` (synchronous — children pre-populated)
- `DeepLoadAndIncludeAsync` → `ApplyIncludedToAllDescendants` (synchronous)
- Children added via `AddChild(vm)` at tree-build time in `AccountFilesViewModel`

### `AccountFilesViewModel`

- After auth: call `GetRootFoldersAsync` (root level) and `GetAllFoldersAsync` (full flat list)
- Build `childrenByParentId` dictionary
- New `BuildFolderNodeTree(folder, childrenByParentId, depth)` recursive helper
- Set `HasChildren = false` when folder has no children in the fetched data
- Wire all child events (`IncludeToggled`, `ChildStateChanged`, etc.) during tree build

### `SyncService`

- Remove `folderRelativePath` argument from `GetDeltaAsync` call

### Tests

- `GivenAFolderTreeNodeViewModel`: remove lazy-load cases, add pre-loaded children cases
- `GraphServiceTests`: add `GetAllFoldersAsync` tests; add unified-delta enumeration tests
- `GivenASyncServiceSyncingAnAccount`: update `GetDeltaAsync` mock call signatures

## Performance Consideration

`GetAllFoldersAsync` makes O(folder-depth × breadth) API calls. Acceptable for personal OneDrive (typically hundreds of folders). For large drives, a future optimisation would be to use the delta API on the root with `$select=id,name,folder,parentReference` and filter client-side, which reduces the sweep to 1–few paginated calls at the cost of receiving file entries too.
