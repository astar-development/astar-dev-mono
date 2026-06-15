## Context

The OneDrive Sync Client stores synced file metadata in `SyncedItemEntity` (SQLite via EF Core) and classification tags in `SyncedItemClassificationEntity`. The `SyncedItemRepository` currently provides only bulk-load (`GetAllByAccountAsync`) and per-item CRUD operations — there is no query API. File size exists in the domain (`FileDeltaItem.size`, `SyncFileMetadata.FileSize`) but is not persisted. The UI has no search surface; users navigate files only via the folder tree.

The `File.App` desktop app already contains `FileTypeClassifier` (extension → `FileType` enum) and `FileViewerService` (opens a file, tracks view history). Those are in a separate project and tied to `File.App`'s EF model, so they cannot be referenced directly; analogous types will be added to the sync client.

Avalonia's `Bitmap` can load image files from disk, making on-demand thumbnail generation straightforward without a third-party library.

## Goals / Non-Goals

**Goals:**
- Persist `SizeInBytes` on `SyncedItemEntity` so it is available for filtering and duplicate detection.
- Provide an in-process SQLite query via `ISyncedItemRepository.SearchAsync` covering name, size range, classification tag, and duplicate-flag filters.
- Display results as cards with 150×150 Avalonia `Bitmap` thumbnails for images and SVG/font icons for other types.
- Open local files in the OS default application on card click.
- Integrate the search panel into the existing main-window navigation without disrupting other views.

**Non-Goals:**
- Remote (Graph API) search — all filtering runs against the local SQLite cache only.
- Full-text content indexing.
- Thumbnail caching to disk.
- Bulk file actions (delete, move) from the search panel.
- Pagination UI controls — virtual scroll (`ItemsRepeater` + `RecyclingElementFactory`) is sufficient for the expected corpus size.

## Decisions

### D1 — Add `SizeInBytes` to `SyncedItemEntity` via EF migration

**Chosen:** New nullable `long? SizeInBytes` column with a default of `null` for pre-existing rows; set to `0` for folders.

**Why:** The column is needed for size-range filtering and duplicate detection. Making it nullable lets the migration succeed without a back-fill pass; search simply treats `null` as "unknown" and excludes those rows when a size filter is active.

**Alternative considered:** Derive size at query time from the local file on disk. Rejected — file may not exist (not yet downloaded), and hitting the file system per row during search is expensive.

### D2 — Filter query implemented as EF Core LINQ in `SyncedItemRepository`

**Chosen:** Build an `IQueryable<SyncedItemEntity>` chain inside `SyncedItemRepository.SearchAsync`, joining to `SyncedItemClassificationEntity` when a tag filter is active, then projecting to a `SyncedItemSearchResult` record.

**Why:** Keeps all data access in the repository layer; EF Core translates LINQ to parameterised SQL, avoiding raw SQL strings. The duplicate filter uses a self-join on `(AccountId, SizeInBytes, derived FileName)` — achievable via a grouped subquery in LINQ.

**Alternative considered:** Pull all rows and filter in memory. Rejected — corpus can be tens of thousands of files; memory cost and latency are unacceptable.

### D3 — Duplicate detection by (FileName + SizeInBytes)

**Chosen:** "Duplicate" means two or more non-folder `SyncedItemEntity` rows for the same account share the same `Path.GetFileName(RemotePath)` value and the same `SizeInBytes`. No hash comparison.

**Why:** Hash storage requires another column and hashing at download time (significant pipeline change). Name+size catches the common case (photo duplicates, re-downloaded files) and is implementable entirely in SQL.

**Limitation:** False positives are possible (different files, same name and size). Documented as a known limitation — acceptable for v1.

### D4 — Thumbnail generation via Avalonia `Bitmap`, loaded on demand in the ViewModel

**Chosen:** `SyncedFileResultViewModel` exposes `IImage? Thumbnail { get; }`. The setter is called lazily when the card scrolls into view (via `Loaded` event or an `IntersectionBehavior`). For image files, load `new Bitmap(localPath)` on a thread-pool thread, then marshal back to UI thread via `Dispatcher.UIThread.InvokeAsync`. Cap loaded dimension at 150px using `Bitmap.DecodeToWidth`.

**Why:** Avalonia `Bitmap.DecodeToWidth` scales during decode, keeping memory low. No third-party library needed.

**Alternative considered:** Use `SkiaSharp` for thumbnail generation (already a transitive dependency via Avalonia). Rejected — overkill; `Bitmap.DecodeToWidth` is sufficient and keeps the dependency surface minimal.

### D5 — File open via new `IFileOpenerService` / `FileOpenerService`

**Chosen:** Single-method service `OpenFile(string localPath)` using the same cross-platform `Process.Start` pattern as the existing `FileManagerService` (`xdg-open` / `open` / `explorer`). Registered as transient in DI.

**Why:** Mirrors the existing pattern; avoids modifying `IFileManagerService` (single-responsibility). The sync client does not have a `FileViewerService` equivalent yet, and unlike `File.App`'s version there is no view-history database to update.

### D6 — New `SyncedFileSearchView` as a top-level navigation destination

**Chosen:** Add a "Search" entry to `MainWindowViewModel`'s navigation list alongside Accounts, Files, Activity, Settings. The view contains a criteria panel (top, `Auto` row) and a results `ScrollViewer` + `ItemsRepeater` (`*` row) per the Avalonia ScrollViewer bounded-viewport rule.

**Why:** Keeps navigation consistent with existing patterns; the view is self-contained and doesn't depend on account tab state.

## Risks / Trade-offs

- **Migration on large existing databases** — adding a nullable column to `SyncedItems` is an instant DDL operation in SQLite (no table rewrite). `SizeInBytes` will be `null` for all pre-existing rows; duplicate filter and size filter silently exclude them until the next full sync populates the column. → Acceptable degraded-mode behaviour; no data loss.
- **Thumbnail load latency** — large RAW/HEIC files can take seconds to decode even with `DecodeToWidth`. → Load on background thread; show a spinner placeholder until done.
- **`SizeInBytes` for upload jobs** — `CreateFromUploadJob` derives size from `IFileSystem.FileInfo.New(localPath).Length`, which requires the file to exist locally at registration time. This is always true for uploads. → No risk.
- **False-positive duplicates** — name+size matching is not collision-resistant. → Display a warning in the UI: "Duplicates are identified by name and size. Verify before deleting."
- **SQLite `Path.GetFileName` in LINQ** — EF Core cannot translate `Path.GetFileName(e.RemotePath)` to SQL. → Use `e.RemotePath.Substring(e.RemotePath.LastIndexOf('/') + 1)` or load candidate rows into memory after a name-prefix pre-filter.

## Migration Plan

1. Add EF Core migration `AddSizeInBytesToSyncedItems` — adds nullable `SizeInBytes long` column, index on `(AccountId, SizeInBytes)`.
2. Existing rows get `null`; no back-fill required.
3. `SyncedItemEntityFactory` updated — future syncs populate `SizeInBytes`.
4. New UI views and services registered — no breaking changes to existing navigation or sync pipeline.
5. Rollback: revert migration (`dotnet ef database update <previous>`); remove new column.

## Open Questions

- Should the search panel be per-account (scoped to the active tab's `AccountId`) or global across all accounts? → Assumed global (all accounts) for v1; an account filter can be added later.
- Virtual scroll vs. explicit pagination — `ItemsRepeater` with `RecyclingElementFactory` is preferred but adds complexity. If a 500-item result set is the realistic ceiling, a simple `ListBox` may suffice. → Decide at implementation time based on profiling.
