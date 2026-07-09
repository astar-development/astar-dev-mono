# Unify file classifications under a single file-keyed table

## Requirement

Scraper and OneDrive Sync must share one set of classifications per physical file. If the Scraper has classified a downloaded file, OneDrive Sync (upload, download, or phantom registration) must reuse those classifications. Any app only classifies a file that has never been classified. No duplication of classification rows per file.

## Decisions (agreed 2026-07-03)

- File identity: `FileDetailEntity`, matched by local path (`DirectoryName` + separator + `FileName`).
- Junction table renamed `FileClassifications`, keyed `FileDetailId` + `CategoryId` only. `SyncedItemId` removed.
- `SyncedItemEntity` gains nullable `FileDetailId` FK linking sync metadata to the canonical file.
- Skip once classified: any existing rows for a `FileDetail` are final; no delete-and-rewrite on sync passes.
- Existing data: union of categories from both parents is preserved by the migration.

## Phase 1 — AppDb schema + data migration (issue #705)

- Rename `SyncedItemFileClassifications` → `FileClassifications`; entity `FileClassificationEntity`; DbSet `FileClassifications`.
- Drop `SyncedItemId`, `SyncedItem` navigation, exactly-one-parent check constraint, filtered indexes. `FileDetailId` non-nullable, plain unique `(FileDetailId, CategoryId)`.
- Add `SyncedItems.FileDetailId` (nullable guid FK → `FileDetail`, `SetNull` on delete).
- Migration data motion (SQL, separator-agnostic path split):
  1. Map each SyncedItem-parented row's `SyncedItems.LocalPath` to an existing `FileDetail` by `DirectoryName`/`FileName`.
  2. Create missing `FileDetail` rows (plus required `FileAccessDetail`, `ImageDetail`, `DeletionStatus` rows).
  3. `INSERT OR IGNORE` converted rows (union with Scraper rows), delete legacy SyncedItem-parented rows.
  4. Backfill `SyncedItems.FileDetailId` from the same mapping.

## Phase 2 — Shared FileDetail resolver (issue #706)

- `IFileDetailResolver` in `AStar.Dev.Infrastructure.AppDb`: `FindOrCreateAsync(fullPath, fileSize, ct)` — find by `DirectoryName` + `FileName`, else create with owned rows.
- Used by OneDrive registrar (and available to Scraper).

## Phase 3 — OneDrive sync (issue #707)

- `SyncedItemRegistrar` (phantom, download, upload): resolve `FileDetail` from local path, set `SyncedItem.FileDetailId`, classify only when the `FileDetail` has zero `FileClassifications` rows.
- `SyncedItemRepository`: remove delete-and-rewrite classification upserts; search tags and `GetDistinctTagNamesAsync` join `SyncedItems.FileDetailId` → `FileClassifications`.
- `ClassificationDataMigrationService` (legacy `SyncedItemClassifications` import) writes FileDetail-keyed rows via the resolver.

## Phase 4 — Scraper skip rule (issue #708)

- `FileClassificationService.ClassifyAsync`: skip when the `FileDetail` already has classification rows; otherwise unchanged.

## Definition of done

Zero build warnings, full suite green, both apps verified writing/reading the same rows for the same path.
