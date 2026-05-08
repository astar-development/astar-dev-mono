## Context

`AStar.Dev.OneDrive.Sync.Client` already uses `Result<T, TError>` from `AStar.Dev.Functional.Extensions` at the auth layer (`IAuthService` returns `Result<AuthResult, AuthError>`). The infrastructure layer beneath it — `IGraphService`, `IUploadService`, `IHttpDownloader`, `DownloadWorker`, `SyncService` — still throws exceptions for predictable failures (null Graph API responses, exhausted retries, missing local files). Callers swallow these via broad `catch(Exception)` blocks in `DownloadWorker.RunAsync` and `SyncService.SyncAccountAsync`.

All callers are in-tree. No external consumers of these interfaces exist.

## Goals / Non-Goals

**Goals:**

- Replace all recoverable `throw` statements in infrastructure with `Result<T, string>` return values
- Update all callers to use `Match`/`Map`/`Bind` — no error messages lost
- Update interface signatures: `IUploadService`, `IHttpDownloader`, `IGraphService`
- Update `AccountFilesViewModel` to use `Match` on `DriveId` option
- All existing tests updated to assert on `Result` outcomes, not caught exceptions

**Non-Goals:**

- Changing the typed `AuthError` discriminated union — auth layer is already correct
- Removing `NotSupportedException` throws in Avalonia `IValueConverter.ConvertBack` — required by Avalonia contract
- Removing `UnreachableException` in `SyncRepository` switch arm — programming invariant, not runtime failure
- Removing `FeatureAvailabilityService` guard throw — programming-error detection, not recoverable failure

## Decisions

### D1 — Error type is `string`, not a typed discriminated union

**Choice:** `Result<T, string>` throughout infrastructure.

**Rationale:** Auth already has a typed `AuthError` union because callers branch on error kind (silent fail vs. show UI vs. force re-auth). Infrastructure errors (Graph API gaps, upload exhaustion) are terminal for that operation — callers log and surface the message string, never branch on error kind. A typed union would add indirection with no consumer benefit.

**Alternative considered:** `Result<T, Exception>` — rejected because it re-introduces exception semantics as data and forces callers to `.Message` everything anyway.

### D2 — `IGraphService` surface area — only methods that currently throw are changed

**Choice:** Change only `GetDriveIdAsync`, `GetRootFoldersAsync`, `GetChildFoldersAsync`, `GetQuotaAsync`, `EnumerateFolderAsync`, `GetDownloadUrlAsync`, `UploadFileAsync`, `DeleteItemAsync` to return `Result`-wrapped types. `GetFolderIdByPathAsync` already returns `string?` (null = not found, no throw) — leave unchanged.

**New signatures:**
- `Task<Result<DriveId, string>> GetDriveIdAsync(...)`
- `Task<Result<List<DriveFolder>, string>> GetRootFoldersAsync(...)`
- `Task<Result<List<DriveFolder>, string>> GetChildFoldersAsync(...)`
- `Task<Result<(long Total, long Used), string>> GetQuotaAsync(...)`
- `Task<Result<List<DeltaItem>, string>> EnumerateFolderAsync(...)`
- `Task<Result<string, string>> GetDownloadUrlAsync(...)` (was `string?` — null case becomes `Error`)
- `Task<Result<string, string>> UploadFileAsync(...)`
- `Task<Result<Unit, string>> DeleteItemAsync(...)`

### D3 — `IUploadService` and `IHttpDownloader` return Result

**New signatures:**
- `IUploadService.UploadAsync`: `Task<string>` → `Task<Result<string, string>>`
- `IHttpDownloader.DownloadAsync`: `Task` → `Task<Result<Unit, string>>`

Internal throws inside these implementations become early `return Result.Error("...")` returns. The outer `while(true)` retry loops propagate `Result.Error` when retry budget is exhausted instead of throwing `HttpRequestException`.

### D4 — `DownloadWorker` uses `Match` on `ResolveDownloadUrlAsync`

`ResolveDownloadUrlAsync` returns `Task<Result<string, string>>`. The caller uses:
```csharp
var urlResult = await ResolveDownloadUrlAsync(downloadJob, accessToken, ct);
return urlResult.Match(
    url  => /* proceed with download, return updated job */,
    err  => /* log + return failed job — outer RunAsync marks as Failed */);
```
The existing `catch(Exception)` in `RunAsync` handles all other unhandled exceptions; `Result` covers the predictable URL-missing case.

### D5 — `SyncService.ApplyConflictOutcomeAsync` uses `Bind`/`Match`

```csharp
var result = await graphService.GetDownloadUrlAsync(accessToken, ..., ct);
await result.Match(
    async url  => await httpDownloader.DownloadAsync(url, ..., ct),
    async err  => { Serilog.Log.Error(...); /* RaiseProgress with error */ });
```

### D6 — `AccountFilesViewModel` removes inline throw from `Option.Match`

Replace:
```csharp
() => throw new InvalidOperationException("Drive ID not available.")
```
With a no-op or log — the caller already guards against `_driveId` being `None` via UI state (button disabled when not loaded). If `None` is reached anyway, log a warning and return early rather than crash.

### D7 — `GraphService.ResolveClientWithDriveContextAsync` returns Result internally

Private method becomes `Task<Result<(GraphServiceClient, DriveContext), string>>`. All callers inside `GraphService` use `Bind`/`Map` to chain. Public methods propagate `Result.Error` on missing drive/root ID instead of throwing.

## Risks / Trade-offs

- **Async Match complexity** → Mitigation: `AStar.Dev.Functional.Extensions` provides `MatchAsync`/`BindAsync` — use these throughout; avoid `.Result` or manual `await` patterns inside lambdas
- **Test churn** → tests asserting `Assert.ThrowsAsync<InvalidOperationException>` become `result.ShouldBeError()` assertions; scope is bounded to the affected test classes
- **`DownloadWorker.RunAsync` already catches `Exception`** — after this change the broad catch becomes narrower since upload/download paths no longer throw predictable errors; `OperationCanceledException` re-throw path unchanged

## Migration Plan

1. Change `IUploadService`, `IHttpDownloader`, `IGraphService` interfaces
2. Update `UploadService`, `HttpDownloader`, `GraphService` implementations
3. Update `DownloadWorker`, `SyncService`, `AccountFilesViewModel` callers
4. Update unit tests: replace `ThrowsAsync` assertions with `Result.Error` assertions
5. `dotnet build` + `dotnet test` — zero errors, zero warnings required before PR
6. Single PR — all callers in-tree, no staged rollout needed
