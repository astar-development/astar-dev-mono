## 1. Feature Branch

- [x] 1.1 Create branch `feature/cascade-folder-selection-with-exclusions` from `main`

## 2. Data Model — SyncFolderEntity

- [x] 2.1 Add `bool IsExplicitlyExcluded` property to `SyncFolderEntity` (default `false`)
- [x] 2.2 Update `SyncFolderEntityConfiguration` to map the new column with a default value of `false`
- [x] 2.3 Add EF Core migration `CascadeSelectionExplicitExclusion` and verify snapshot

## 3. FolderTreeNodeViewModel — Cascade & Inheritance

- [x] 3.1 Add `FolderSyncState InheritedState` property (internal; not observable) representing what new children should start as
- [x] 3.2 Add `ChildStateChanged` event raised whenever `SyncState` changes on a node
- [x] 3.3 Implement `CascadeStateToDescendants(FolderSyncState state)` — walks loaded `Children` recursively, sets `SyncState` and `InheritedState` on each
- [x] 3.4 Update `ToggleInclude` to call `CascadeStateToDescendants` after toggling self, then raise `ChildStateChanged`
- [x] 3.5 Subscribe parent to `ChildStateChanged` of each child when children are created (in `EnsureChildrenLoadedAsync` and when root nodes are built)
- [x] 3.6 Implement `RecalculateStateFromChildren()` on parent: `Included` if all children included, `Excluded` if all excluded, `Partial` otherwise; called on `ChildStateChanged`
- [x] 3.7 In `EnsureChildrenLoadedAsync`, initialise each new child's `SyncState` from parent's `InheritedState` (overridden later if persisted exclusion exists)

## 4. AccountFilesViewModel — Persistence

- [x] 4.1 Replace `CollectAllIncluded` with `CollectSyncDecisions` that yields `(FolderTreeNodeViewModel node, bool isExplicitExclusion)` tuples — included/partial nodes yield `false`, explicitly-excluded nodes under an included/partial parent yield `true`
- [x] 4.2 Update `OnIncludeToggled` to build `SyncFolderEntity` list from `CollectSyncDecisions` setting `IsExplicitlyExcluded` accordingly
- [x] 4.3 Update `LoadAsync` to pass persisted explicit exclusion IDs into each root `FolderTreeNodeViewModel` so children can check on lazy load (store as a `HashSet<OneDriveFolderId>` on `AccountFilesViewModel`)
- [x] 4.4 Update `EnsureChildrenLoadedAsync` (or pass the exclusion set through) so newly loaded children override `InheritedState` with `Excluded` when their id appears in the explicit exclusion set

## 5. Tests

- [x] 5.1 Create/extend unit test project for `FolderTreeNodeViewModel` cascade behaviour (include cascades, exclude cascades, partial state propagation)
- [x] 5.2 Test inherited state: children of included parent start as included; children of excluded parent start as excluded
- [x] 5.3 Test explicit exclusion override: child starts as excluded when id in exclusion set, even under included parent
- [x] 5.4 Test `CollectSyncDecisions` yields correct flags for mixed-state trees
- [x] 5.5 Test `RecalculateStateFromChildren` produces `Partial`, `Included`, and `Excluded` correctly

## 6. Build & Review

- [x] 6.1 `dotnet build` — zero errors, zero warnings
- [x] 6.2 `dotnet test` — all tests pass
- [ ] 6.3 Request human code review before committing
- [ ] 6.4 Implement review feedback, re-run build & tests
- [ ] 6.5 Commit and raise GitHub PR once review approved
