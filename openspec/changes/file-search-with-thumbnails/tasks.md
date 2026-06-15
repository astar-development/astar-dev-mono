## 1. Data Layer — SizeInBytes Column

- [x] 1.1 Write failing unit tests for all three `SyncedItemEntityFactory` overloads asserting `SizeInBytes` is set correctly (RED commit)
- [x] 1.2 Add `SizeInBytes long?` property to `SyncedItemEntity`
- [x] 1.3 Add EF Core migration `AddSizeInBytesToSyncedItems` (nullable column + index on `AccountId, SizeInBytes`)
- [x] 1.4 Update `SyncedItemEntityConfiguration` if needed for the new column
- [x] 1.5 Update `SyncedItemEntityFactory.Create(AccountId, FileDeltaItem, ...)` to set `SizeInBytes = item.Size`
- [x] 1.6 Update `SyncedItemEntityFactory.CreateFromDownloadJob` to set `SizeInBytes` from `SyncFileMetadata.FileSize`
- [x] 1.7 Update `SyncedItemEntityFactory.CreateFromUploadJob` to set `SizeInBytes` from `IFileSystem.FileInfo.New(localPath).Length`
- [x] 1.8 Verify all 1.1 tests pass (GREEN)

## 2. Repository — Search Query

- [x] 2.1 Define `SyncedItemSearchCriteria` record (NameFragment, MinBytes, MaxBytes, Tags, DuplicatesOnly, AccountId)
- [x] 2.2 Define `SyncedItemSearchResult` record (entity fields needed for display + list of TagNames)
- [x] 2.3 Write failing unit tests for `SyncedItemRepository.SearchAsync` covering: name filter, size range, tag filter, duplicates toggle, combined criteria, folder exclusion, null-size exclusion when size filter active (RED commit)
- [x] 2.4 Add `SearchAsync(SyncedItemSearchCriteria, CancellationToken)` to `ISyncedItemRepository`
- [x] 2.5 Implement `SearchAsync` in `SyncedItemRepository` with LINQ query chain (name, size range, tag join, duplicate sub-query, folder exclusion)
- [x] 2.6 Verify all 2.3 tests pass (GREEN)

## 3. File Opener Service

- [x] 3.1 Define `IFileOpenerService` with `OpenFile(string localPath)` method
- [x] 3.2 Write failing unit tests for `FileOpenerService` verifying cross-platform launcher selection and no-op when file missing (RED commit)
- [x] 3.3 Implement `FileOpenerService` using cross-platform `Process.Start` (mirrors `FileManagerService`)
- [x] 3.4 Register `IFileOpenerService` / `FileOpenerService` as transient in DI
- [x] 3.5 Verify all 3.2 tests pass (GREEN)

## 4. File Type Classification (OneDrive Client)

- [x] 4.1 Define `IFileTypeClassifier` interface in the OneDrive sync client
- [x] 4.2 Write failing unit tests for `SyncClientFileTypeClassifier` covering image, document, video, audio, archive, code, unknown extensions (RED commit)
- [x] 4.3 Implement `SyncClientFileTypeClassifier` mapping extensions to `FileType` enum (same extension map as `File.App`'s `FileTypeClassifier`)
- [x] 4.4 Register `IFileTypeClassifier` / `SyncClientFileTypeClassifier` in DI
- [x] 4.5 Verify all 4.2 tests pass (GREEN)

## 5. Search ViewModel

- [x] 5.1 Define `SyncedFileResultViewModel` and `SyncedFileSearchViewModel` skeletons (properties only, no behaviour)
- [x] 5.2 Write failing unit tests for `SyncedFileSearchViewModel`: criteria building, result mapping, `ResultCount` updates, `ShowDuplicateDisclaimer` toggling, disabled open when local file missing (RED commit)
- [x] 5.3 Implement `SyncedFileResultViewModel` with properties: FileName, FormattedSize, TagName, LocalPath, FileType, IsLocalPresent, Thumbnail (`IImage?`)
- [x] 5.4 Implement lazy thumbnail loading in `SyncedFileResultViewModel`: decode on background thread via `Bitmap.DecodeToWidth(150)`, marshal to UI thread, placeholder while loading
- [x] 5.5 Implement `SyncedFileSearchViewModel` with: criteria properties (NameFragment, MinSize, MaxSize, SelectedTags, DuplicatesOnly), `SearchCommand`, `Results`, `ResultCount`, `AvailableTags`, `IsSearching`, `ShowDuplicateDisclaimer`
- [x] 5.6 Implement `SearchCommand` handler: build criteria, call `SearchAsync`, map to `SyncedFileResultViewModel` list
- [x] 5.7 Implement `OpenFileCommand` on `SyncedFileResultViewModel` calling `IFileOpenerService.OpenFile` when `IsLocalPresent`
- [x] 5.8 Verify all 5.2 tests pass (GREEN)

## 6. Search View (Avalonia XAML)

- [x] 6.1 Create `SyncedFileSearchView.axaml` with criteria panel (`Auto` row) and results `ScrollViewer` + `ItemsRepeater` (`*` row) per bounded-viewport rule
- [x] 6.2 Add name text field, size min/max numeric fields, tag multi-select, duplicates toggle, Search button to criteria panel
- [x] 6.3 Add result card `DataTemplate` showing thumbnail/icon (150×150), filename, formatted size, tag name
- [x] 6.4 Add result count badge bound to `ResultCount`
- [x] 6.5 Add duplicate disclaimer `TextBlock` bound to `ShowDuplicateDisclaimer`
- [x] 6.6 Style disabled card state when `IsLocalPresent = false`
- [x] 6.7 Register `SyncedFileSearchView` / `SyncedFileSearchViewModel` pair in `ViewLocator`

## 7. Navigation Integration

- [x] 7.1 Write failing unit/integration test asserting Search nav entry exists in `MainWindowViewModel` (RED commit)
- [x] 7.2 Add `Search` navigation entry to `MainWindowViewModel` nav list
- [x] 7.3 Register `SyncedFileSearchViewModel` in DI (`ViewModelExtensions`)
- [x] 7.4 Verify all 7.1 tests pass and existing nav items unaffected (GREEN)
