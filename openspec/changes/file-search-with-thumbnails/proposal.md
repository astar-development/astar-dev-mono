## Why

Users have no way to locate specific synced files without browsing the folder tree; there is no search, no size filter, and no way to spot duplicates. A dedicated search panel with visual previews closes that gap and makes the synced-file corpus immediately navigable.

## What Changes

- Add a **Search** navigation entry to the OneDrive Sync Client main window.
- Add a `SizeInBytes` column to `SyncedItemEntity` and a corresponding EF Core migration.
- Populate `SizeInBytes` in `SyncedItemEntityFactory` from the already-available `FileDeltaItem.size` / `SyncFileMetadata.FileSize` values.
- Extend `ISyncedItemRepository` with a `SearchAsync` method that accepts a filter criteria record and returns a paged list of matching entities with their classifications.
- Add `IFileOpenerService` / `FileOpenerService` to open a local file in the OS default application (mirrors the existing `FileManagerService` pattern using `xdg-open` / `open` / `explorer`).
- Add `SyncedFileSearchViewModel`, `SyncedFileResultViewModel`, and `SyncedFileSearchView` (Avalonia) implementing the search UI.
- Register all new types in DI; wire the new view into the navigator.

## Capabilities

### New Capabilities

- `synced-file-search`: Multi-criteria filter panel for synced files. Criteria — name (partial, case-insensitive), size range (min bytes, max bytes), one or more classification tags, and a "duplicates only" toggle. All active criteria are AND-joined. Results display in a scrollable grid with a count badge. Paging or virtual scroll keeps memory bounded.
- `file-result-display`: Each search result card shows a 150×150 px thumbnail (loaded from `LocalPath`) for image files, and a file-type icon (derived from `FileTypeClassifier`-equivalent extension mapping) for all other types. Clicking the card opens the local file via the OS default application. Unavailable local files (not yet downloaded) show a placeholder icon and disable click-to-open.

### Modified Capabilities

- `file-classification`: No requirement changes — classification data is read-only in this feature. The existing `SyncedItemClassificationEntity` rows are the source for the tag filter.

## Impact

- **`SyncedItemEntity`** — new `SizeInBytes long` column; requires EF migration.
- **`SyncedItemEntityFactory`** — all three `Create`/`CreateFromDownloadJob`/`CreateFromUploadJob` overloads must set `SizeInBytes` (available via `FileDeltaItem.size`, `SyncFileMetadata.FileSize`, and `IFileSystem.FileInfo` respectively).
- **`ISyncedItemRepository` / `SyncedItemRepository`** — new `SearchAsync(SyncedItemSearchCriteria, CancellationToken)` returning `IReadOnlyList<SyncedItemSearchResult>` (entity + classifications joined).
- **New `IFileOpenerService` / `FileOpenerService`** — single `OpenFile(string localPath)` method; cross-platform via `Process.Start`.
- **New search ViewModel/View layer** — `SyncedFileSearchViewModel`, `SyncedFileResultViewModel`, `SyncedFileSearchView.axaml`.
- **`MainWindowViewModel`** — add Search nav item.
- **`ViewModelExtensions`** — register new services and ViewModels.
- **No changes to sync pipeline, classification rules, or authentication.**
