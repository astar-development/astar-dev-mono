## Context

The OneDrive Sync Client displays a lazily-loaded folder tree. Each node (`FolderTreeNodeViewModel`) has a `SyncState` and a toggle command. Currently toggling a node affects only that node; children are always created with `FolderSyncState.Excluded`. Persisted sync folders are stored in `SyncFolderEntity` rows; only included folders are saved. There is no concept of an explicitly persisted exclusion.

## Goals / Non-Goals

**Goals:**
- Cascade include/exclude state down the in-memory tree to all loaded descendants when a toggle fires.
- New children loaded under an already-included parent default to `Included`.
- Explicit sub-folder exclusion (child excluded while parent included) is persisted and restored on next load.
- Parent displays `Partial` when any descendant is explicitly excluded.

**Non-Goals:**
- Cascade to unloaded sub-trees beyond setting an inherited state flag — we do not force-load the whole tree.
- UI changes beyond the existing per-node toggle button.
- Changing conflict-resolution or sync-engine behaviour.

## Decisions

### D1 — Inherited state flag on `FolderTreeNodeViewModel`

Add a `FolderSyncState InheritedState` property to `FolderTreeNodeViewModel`. When children are loaded lazily, they read the parent's `InheritedState` and start in that state instead of always `Excluded`.

**Alternative considered**: Pass the parent VM reference into each child and read `SyncState` directly. Rejected — creates tight coupling and circular event chains.

### D2 — Cascade via recursive tree walk in `ToggleInclude`

`ToggleInclude` toggles `SyncState` then calls a new `CascadeStateToDescendants(FolderSyncState state)` method that walks `Children` recursively and sets each child's `SyncState` and `InheritedState`. Unloaded children (not yet in `Children`) only receive the `InheritedState` update so they pick it up on lazy load.

**Alternative considered**: Bubble events upward from children to parent to recalculate `Partial`. Kept as part of D3 only for upward propagation; downward propagation is simpler as a direct walk.

### D3 — `Partial` state propagation via parent callback

`FolderTreeNodeViewModel` exposes a `ChildStateChanged` event. The parent subscribes when a child is added (already-loaded or lazy-loaded). On receiving the event the parent re-evaluates its own `SyncState`: if all children are `Included` → `Included`; if all `Excluded` → `Excluded`; otherwise → `Partial`.

### D4 — Persist explicit exclusions in `SyncFolderEntity`

Add `bool IsExplicitlyExcluded` to `SyncFolderEntity`. `AccountFilesViewModel.CollectAllIncluded` is replaced by `CollectSyncDecisions` which yields:
- All `Included`/`Partial` nodes → `IsExplicitlyExcluded = false`
- All nodes where parent is `Included` or `Partial` but the node itself is `Excluded` → `IsExplicitlyExcluded = true`

On reload, a node whose id appears as `IsExplicitlyExcluded=true` starts as `Excluded` even when the parent's `InheritedState` is `Included`.

**Alternative considered**: Store only included folders plus a separate exclusion table. Rejected — adds table and join complexity for a flag that fits naturally on the existing entity.

### D5 — EF Core migration for the new column

Standard `dotnet ef migrations add CascadeSelectionExplicitExclusion` in the infra project. Default value `false` so existing rows are unaffected.

## Risks / Trade-offs

- **Unloaded sub-trees**: Exclusions set on children that have never been expanded are not persisted because those nodes were never in memory. Mitigation: the `InheritedState` mechanism ensures they start in the correct state when eventually loaded; users must explicitly exclude them after expanding.
- **Large trees**: Deep recursive cascade over a fully-expanded tree is O(n). For typical OneDrive folder counts (<1000 visible nodes) this is negligible. Mitigation: document limit; revisit if perf reports emerge.
- **Partial persistence round-trip**: On reload, `Partial` parent state must be recomputed from the persisted child states rather than stored directly. Mitigation: `LoadAsync` sets each node's `SyncState` from persisted data; after all root nodes are built, `Partial` bubbles up automatically via `ChildStateChanged`.

## Migration Plan

1. Add `IsExplicitlyExcluded` column with default `false` via EF migration.
2. Existing data: no row is an explicit exclusion, so all existing sync behaviour is preserved.
3. Rollback: remove the migration and column — no data loss as the column was additive.
