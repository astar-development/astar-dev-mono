## Why

`AStar.Dev.OneDrive.Sync.Client` throws exceptions for predictable failure conditions (missing download URLs, Graph API gaps, upload exhaustion, missing files) — callers catch them with broad `catch(Exception)` blocks, hiding intent and making error propagation invisible in method signatures. Replacing throws with `Result<T, TError>` from `AStar.Dev.Functional.Extensions` makes failures first-class, enables `Match`/`Map`/`Bind` composition, and ensures users still see error messages without relying on exception semantics.

## What Changes

- `IUploadService.UploadAsync` returns `Task<Result<string, string>>` instead of `Task<string>`
- `IHttpDownloader.DownloadAsync` returns `Task<Result<Unit, string>>` instead of `Task`
- `IGraphService` — internal `ResolveClientWithDriveContextAsync` propagates `Result` so `GetDriveIdAsync`, `UploadFileAsync`, `DeleteItemAsync`, `GetRootFoldersAsync`, `GetChildFoldersAsync`, `GetQuotaAsync`, `EnumerateFolderAsync`, `GetDownloadUrlAsync` all return `Result`-wrapped types
- `DownloadWorker.ResolveDownloadUrlAsync` returns `Result<string, string>` and uses `Match` to either proceed or mark job failed
- `SyncService.ApplyConflictOutcomeAsync` uses `Bind`/`Match` on the download URL result instead of null-coalescing throw
- `AccountFilesViewModel` uses `Match` on `DriveId` availability instead of inline throw
- `SyncRepository` exhaustive switch arm reviewed — `UnreachableException` retained (programming invariant, not runtime failure)
- Avalonia `IValueConverter.ConvertBack` throws (`NotSupportedException`) retained — required by Avalonia contract for one-way converters
- `FeatureAvailabilityService` guard throw retained — programming-error guard, not a recoverable runtime failure

## Capabilities

### New Capabilities

- `result-error-handling-in-sync-infrastructure`: Replace all recoverable `throw` statements in the sync/graph/download/upload infrastructure with `Result<T, string>`, surfacing error messages to callers via `Match`/`Map`/`Bind` rather than exception propagation

### Modified Capabilities

- `local-sync-path-value-object`: No requirement change — implementation detail only if `LocalSyncPath` factory methods surface validation errors via Result

## Impact

- `IUploadService`, `IHttpDownloader`, `IGraphService` interface signatures change — all callers updated in same PR
- `DownloadWorker`, `SyncService`, `AccountFilesViewModel`, `GraphService` updated to consume Result
- All existing tests covering these paths must be updated to assert on `Result` outcomes rather than exception throws
- No user-facing error messages removed — all error strings preserved as `Result.Error` values and surfaced via `Match`
