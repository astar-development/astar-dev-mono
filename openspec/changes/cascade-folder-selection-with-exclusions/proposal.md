## Why

Users selecting a OneDrive folder expect all sub-folders to sync automatically; manually selecting every child folder is tedious and error-prone. Users also need to exempt specific sub-folders from syncing without de-selecting the whole parent tree.

## What Changes

- Toggling a folder to **Included** cascades that state to all already-loaded descendants.
- Children loaded lazily (on expand) under an Included parent open in the **Included** state by default.
- Toggling a folder to **Excluded** cascades that state to all already-loaded descendants.
- A parent folder whose children have mixed inclusion shows **Partial** state.
- The existing per-node include/exclude toggle becomes the mechanism for explicit sub-folder exclusion — no new UI control needed.
- Explicitly excluded sub-folders under an included parent are persisted so the exclusion survives app restarts.
- `SyncFolderEntity` gains an `IsExplicitlyExcluded` flag to distinguish stored exclusions from stored inclusions.
- A new EF Core migration captures the schema change.

## Capabilities

### New Capabilities

- `cascade-folder-selection`: When a folder's sync state is toggled, the new state propagates to all loaded descendants; lazily-loaded descendants inherit the parent state on load.
- `explicit-folder-exclusion`: A sub-folder can be explicitly excluded while its parent is included. Exclusions are persisted and restored on next load. The parent displays `Partial` state when any direct or indirect child is excluded.

### Modified Capabilities

- (none — no existing spec-level requirements change)

## Impact

- `FolderTreeNodeViewModel` — new cascade logic in `ToggleInclude`; parent reference or callback to propagate `Partial` upward; child state inheritance on lazy load.
- `AccountFilesViewModel` — `CollectAllIncluded` replaced by `CollectSyncDecisions` that emits both inclusions and explicit exclusions; `OnIncludeToggled` persists both sets.
- `SyncFolderEntity` — new `IsExplicitlyExcluded bool` column.
- `SyncFolderEntityConfiguration` — configure new column.
- EF Core migration — add `IsExplicitlyExcluded` column.
- No API or NuGet package surface changes.
