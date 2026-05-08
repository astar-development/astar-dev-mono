## 1. Branch and Failing Tests

- [x] 1.1 Create branch `refactor/replace-throws-with-result-onedrive-sync-client`
- [x] 1.2 Write failing test: `UploadService` returns `Result.Error` when local file not found (was `Assert.ThrowsAsync<FileNotFoundException>`)
- [x] 1.3 Write failing test: `UploadService` returns `Result.Error` when Graph API returns no upload session URL
- [x] 1.4 Write failing test: `UploadService` returns `Result.Error` when retry budget exhausted (429)
- [x] 1.5 Write failing test: `UploadService` returns `Result.Error` when upload completes with no item ID
- [x] 1.6 Write failing test: `HttpDownloader` returns `Result.Error` when retry budget exhausted
- [x] 1.7 Write failing test: `GraphService` returns `Result.Error` when Graph API returns null drive ID
- [x] 1.8 Write failing test: `GraphService` returns `Result.Error` when Graph API returns null root item ID
- [x] 1.9 Write failing test: `DownloadWorker` marks job `Failed` when URL resolution returns `Result.Error`
- [x] 1.10 Commit failing tests on branch

## 2. Interface Changes

- [x] 2.1 Update `IUploadService.UploadAsync` return type to `Task<Result<string, string>>`
- [x] 2.2 Update `IHttpDownloader.DownloadAsync` return type to `Task<Result<Unit, string>>`
- [x] 2.3 Update `IGraphService.GetDriveIdAsync` return type to `Task<Result<DriveId, string>>`
- [x] 2.4 Update `IGraphService.GetRootFoldersAsync` return type to `Task<Result<List<DriveFolder>, string>>`
- [x] 2.5 Update `IGraphService.GetChildFoldersAsync` return type to `Task<Result<List<DriveFolder>, string>>`
- [x] 2.6 Update `IGraphService.GetQuotaAsync` return type to `Task<Result<(long Total, long Used), string>>`
- [x] 2.7 Update `IGraphService.EnumerateFolderAsync` return type to `Task<Result<List<DeltaItem>, string>>`
- [x] 2.8 Update `IGraphService.GetDownloadUrlAsync` return type to `Task<Result<string, string>>` (was `string?`)
- [x] 2.9 Update `IGraphService.UploadFileAsync` return type to `Task<Result<string, string>>`
- [x] 2.10 Update `IGraphService.DeleteItemAsync` return type to `Task<Result<Unit, string>>`

## 3. UploadService Implementation

- [x] 3.1 Replace `throw new FileNotFoundException(...)` with `return Result.Error("Local file not found: ...")` in `UploadAsync`
- [x] 3.2 Refactor `CreateSessionWithRetryAsync` to return `Task<Result<string, string>>`; replace ternary throw with `Result.Error`
- [x] 3.3 Refactor `UploadChunksAsync` to return `Task<Result<string, string>>`; replace `throw new InvalidOperationException("Upload completed...")` with `Result.Error`
- [x] 3.4 Refactor `UploadChunkWithRetryAsync` to return `Task<Result<string?, string>>`; replace `throw new HttpRequestException(...)` with `Result.Error`
- [x] 3.5 Refactor `GetUploadedDocumentId` to return `Task<Result<string, string>>`; replace null-coalescing throw with `Result.Error`
- [x] 3.6 Chain all private methods via `Bind`/`Map` in `UploadAsync`

## 4. HttpDownloader Implementation

- [x] 4.1 Change `DownloadAsync` return type from `Task` to `Task<Result<Unit, string>>`
- [x] 4.2 Replace `throw new HttpRequestException(...)` on exhausted retries with `return Result.Error(...)`
- [x] 4.3 Return `Result.Ok(Unit.Value)` on successful download completion

## 5. GraphService Implementation

- [x] 5.1 Refactor `ResolveClientWithDriveContextAsync` to return `Task<Result<(GraphServiceClient, DriveContext), string>>`
- [x] 5.2 Replace `throw new InvalidOperationException("Could not retrieve drive ID.")` with `Result.Error`
- [x] 5.3 Replace `throw new InvalidOperationException("Could not retrieve root item ID.")` with `Result.Error`
- [x] 5.4 Update all callers of `ResolveClientWithDriveContextAsync` inside `GraphService` to use `Bind`/`Map`
- [x] 5.5 Update `GetDownloadUrlAsync` to return `Result.Ok(url)` or `Result.Error("No download URL available for item <id>.")` instead of `string?`
- [x] 5.6 Propagate `Result` wrapping through all remaining `IGraphService` method implementations

## 6. DownloadWorker Implementation

- [x] 6.1 Update `ResolveDownloadUrlAsync` return type to `Task<Result<string, string>>`
- [x] 6.2 Replace null-coalescing throw with `Result.Error` in `ResolveDownloadUrlAsync`
- [x] 6.3 Update `ExecuteJobAsync` download branch to use `Match` on `ResolveDownloadUrlAsync` result — log error and mark job failed on `Error`, proceed on `Ok`
- [x] 6.4 Update `ExecuteJobAsync` upload branch to use `Match` on `IGraphService.UploadFileAsync` result

## 7. SyncService Implementation

- [x] 7.1 Update `ApplyConflictOutcomeAsync` `UseRemote` branch to use `Match`/`Bind` on `GetDownloadUrlAsync` result
- [x] 7.2 On `Result.Error` in conflict resolution: log error and call `RaiseProgress` with `SyncState.Error` and the error message
- [x] 7.3 Update callers of `IGraphService` methods in `SyncService` (if any) to handle `Result` return types

## 8. AccountFilesViewModel Implementation

- [x] 8.1 Replace `() => throw new InvalidOperationException("Drive ID not available.")` in `Option.Match` with a warning log and early return

## 9. Build Verification and Test Fixes

- [x] 9.1 Run `dotnet build` — resolve all compiler errors from interface signature changes
- [x] 9.2 Update all existing unit tests that used `Assert.ThrowsAsync` for the replaced throws to assert `Result.IsError` / `result.ShouldBeError()`
- [x] 9.3 Update any test doubles / mocks (`NSubstitute`) for `IUploadService`, `IHttpDownloader`, `IGraphService` to match new signatures
- [x] 9.4 Run `dotnet test` — all tests pass (new tests from task group 1 now green)

## 10. Review and PR

- [x] 10.1 Request human review — do NOT commit without approval
- [ ] 10.2 Address review feedback
- [x] 10.3 Commit to branch and raise GitHub PR referencing this change
