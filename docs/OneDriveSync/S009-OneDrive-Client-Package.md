# S009 — OneDrive Client Package (Graph API / Folder Browsing / Delta Queries / File Operations)

**Phase:** MVP  
**Area:** packages/infra/astar-dev-onedrive-client  
**Spec refs:** SE-09, SE-10, SE-12, AM-03, Section 9 (OneDrive Client structure)

---

## User Story

As a developer,  
I want a well-tested `AStar.Dev.OneDrive.Client` package that wraps the Microsoft Graph SDK for folder browsing, delta queries, file download, and file upload,  
So that the sync engine and account management feature have a reliable, mockable Graph abstraction.

---

## Acceptance Criteria

### Folder Browsing (AM-03)
- [x] `IOneDriveFolderService` — `GetRootFoldersAsync(accountId, CancellationToken)` and `GetChildFoldersAsync(accountId, folderId, CancellationToken)`
- [x] Returns `Result<IReadOnlyList<OneDriveFolder>>` — never throws
- [x] `OneDriveFolder` record: `Id`, `Name`, `ParentId`, `HasChildren` (uses `ParentId` instead of `Path` for cleaner tree navigation)

### Delta Query Service (SE-09, SE-10, SE-12)
- [x] `IDeltaQueryService` — `GetDeltaAsync(accountId, folderId, deltaToken, CancellationToken)` returns `Result<DeltaQueryResult>`
- [x] `DeltaQueryResult` contains: `IReadOnlyList<DeltaItem>` (changed/added/deleted items), `string NextDeltaToken`, `bool IsFullSync`
- [x] Delta token expiry (HTTP 410 Gone) detected and returned as `DeltaTokenExpiredError` (not an exception)
- [x] `DeltaItem` has `ItemType` distinguishing `File`, `Folder`, `Deleted`, `FolderRenamed` — rename detected via Graph `@microsoft.graph.moveOrRenamed` annotation (SE-12)
- [x] `DeltaToken` value object stored in SQLite per account/folder (wired up in sync engine stories)

### File Download (SE-02, SE-16)
- [x] `IFileDownloader` — `DownloadAsync(accountId, remoteFileId, localPath, IProgress<long>, CancellationToken)`
- [x] Returns `Result<FileDownloadResult>` — never throws
- [x] Respects `CancellationToken` — partial file cleaned up on cancellation
- [x] SE-16 (resumable downloads) is Post-MVP; `IFileDownloader` interface designed to accommodate a `byteOffset` parameter in future without breaking changes

### File Upload
- [x] `IFileUploader` — `UploadAsync(accountId, localPath, remoteFolderId, IProgress<long>, CancellationToken)`
- [x] Returns `Result<FileUploadResult>` — never throws
- [x] Uses Graph chunked upload for files > 4 MB; direct upload for smaller files

### Graph API 429 Handling (EH-02)
- [x] All Graph calls: if HTTP 429 received, silently read `Retry-After` header, delay, and retry (up to 3 retries) — implemented in shared `GraphRetryHelper`; applied in `DeltaQueryService`, `FileDownloader`, and `FileUploader`
- [x] Retry count and delay logged at `Debug` level
- [x] After 3 retries, return `Result.Failure(ThrottledError)`

### Tests (NF-09, NF-10, NF-14)
- [x] All interfaces mocked in tests — no real Graph API calls in unit tests
- [x] Custom `HttpMessageHandler` fakes (`FakeGraphClients`) used to simulate Graph responses (NF-14)
- [x] **Unit test**: `DeltaQueryService` — 410 response returns `DeltaTokenExpiredError`
- [x] **Unit test**: `DeltaQueryService` — folder rename item parsed as `FolderRenamed` type
- [x] **Unit test**: 429 retry — first call 429; second call 200 → result returned; after 3 retries → `ThrottledError` (`GivenAGraphRetryHelper`)
- [x] **Unit test**: `FileDownloader` — cancellation mid-download cleans up partial file
- [x] **Unit test**: `FileUploader` — file > 4 MB uses chunked upload path; < 4 MB uses direct upload path
- [x] `dotnet build` zero errors/warnings; `dotnet test` all pass (38/38)

---

## Technical Notes

- Microsoft Graph SDK (or Kiota) — version pinned in `Directory.Packages.props`
- `GraphServiceClient` wrapped behind `IGraphClientFactory` — factory returns a per-account authenticated client using tokens from `ITokenManager`
- `System.IO.Abstractions` used for all local file operations — no `System.IO.File` directly (NF-10)
- NF-16: all public methods return `Result<T>` or `Option<T>`
- NF-08: HTTPS enforced — `GraphServiceClient` uses the default HTTPS base URL; no HTTP downgrade permitted
- NF-07: no PII (email, account ID) in log messages — account logged only as synthetic `Guid`

---

## Implementation Constraints

- **`ConfigureAwait(false)` on all `await` calls** — this is a library package with no UI dependency; every `await` must use `.ConfigureAwait(false)` to avoid capturing any calling `SynchronizationContext`.
- **`System.IO.Abstractions` for all file I/O** — never call `System.IO.File`, `System.IO.Directory`, or `System.IO.Path` directly; route through the abstraction for testability. A direct `System.IO` call is a test-isolation bug.
- **No `new HttpClient()`** — `GraphServiceClient` must be created via `IGraphClientFactory`; a directly-instantiated `HttpClient` exhausts sockets under repeated construction and is flagged by the `HttpClientFactory` Roslyn analyser.
- **`IProgress<long>` called from background thread** — `IProgress<long>` implementations are invoked from the download/upload thread; do not assume UI thread inside the `Report` callback. The desktop app's progress handler must marshal to `RxApp.MainThreadScheduler`.
---

## Dependencies

- S001 (project scaffolding — package exists)
- S007 (authentication — tokens needed for Graph calls)
