## ADDED Requirements

### Requirement: UploadService returns Result on failure
`IUploadService.UploadAsync` SHALL return `Task<Result<string, string>>`. On success the `Ok` value is the uploaded OneDrive item ID. On failure the `Error` value is a human-readable message. It SHALL NOT throw for predictable failures (missing local file, exhausted retries, missing item ID in Graph API response).

#### Scenario: Local file does not exist
- **WHEN** `UploadAsync` is called with a `localPath` that does not exist on disk
- **THEN** the method returns `Result.Error("Local file not found: <localPath>")` without throwing

#### Scenario: Graph API returns no upload session URL
- **WHEN** the Graph API `createUploadSession` response has a null `UploadUrl`
- **THEN** the method returns `Result.Error("Graph API did not return an upload session URL.")` without throwing

#### Scenario: Retry budget exhausted during chunk upload
- **WHEN** the upload chunk endpoint returns HTTP 429 more than `MaxRetries` consecutive times
- **THEN** the method returns `Result.Error("Upload rate limited after 5 retries.")` without throwing

#### Scenario: Graph API returns no item ID after upload completes
- **WHEN** all chunks are accepted but the final response contains no `id` field
- **THEN** the method returns `Result.Error("Upload completed without receiving item ID from Graph API.")` without throwing

#### Scenario: Successful upload
- **WHEN** `UploadAsync` is called with a valid file and the Graph API accepts all chunks
- **THEN** the method returns `Result.Ok(<itemId>)` containing the remote item ID

### Requirement: HttpDownloader returns Result on failure
`IHttpDownloader.DownloadAsync` SHALL return `Task<Result<Unit, string>>`. On success the `Ok` value is `Unit`. On failure the `Error` value is a human-readable message. It SHALL NOT throw for predictable failures.

#### Scenario: Retry budget exhausted during download
- **WHEN** the download endpoint returns HTTP 429 more than `MaxRetries` consecutive times
- **THEN** the method returns `Result.Error("Rate limited after 5 retries.")` without throwing

#### Scenario: Successful download
- **WHEN** `DownloadAsync` is called with a reachable URL
- **THEN** the method returns `Result.Ok(Unit.Value)` and the file is written to `localPath`

### Requirement: GraphService returns Result on infrastructure failures
All `IGraphService` methods that previously threw `InvalidOperationException` for null Graph API responses SHALL return `Result`-wrapped types. Null or missing data from the Graph API SHALL produce `Result.Error` values rather than exceptions.

#### Scenario: Drive ID unavailable from Graph API
- **WHEN** `GetDriveIdAsync` is called and the Graph API returns a null drive ID
- **THEN** the method returns `Result.Error("Could not retrieve drive ID.")` without throwing

#### Scenario: Root item ID unavailable from Graph API
- **WHEN** `GetDriveIdAsync` (via `ResolveClientWithDriveContextAsync`) succeeds for the drive but the root item has a null ID
- **THEN** the method returns `Result.Error("Could not retrieve root item ID.")` without throwing

#### Scenario: Download URL unavailable for item
- **WHEN** `GetDownloadUrlAsync` is called and the item has no download URL in `AdditionalData`
- **THEN** the method returns `Result.Error("No download URL available for item <itemId>.")` without throwing

#### Scenario: Successful drive ID resolution
- **WHEN** `GetDriveIdAsync` is called and the Graph API returns a valid drive
- **THEN** the method returns `Result.Ok(<driveId>)` and the result is cached for subsequent calls

### Requirement: DownloadWorker handles Result from infrastructure
`DownloadWorker` SHALL use `Match` on the `Result` returned by `ResolveDownloadUrlAsync` and by `IHttpDownloader.DownloadAsync`. On error it SHALL log the message and mark the sync job as `Failed` with the error string. It SHALL NOT rely on exception propagation for these cases.

#### Scenario: Download URL resolution fails
- **WHEN** `ResolveDownloadUrlAsync` returns `Result.Error`
- **THEN** the worker logs the error message, marks the job as `SyncJobState.Failed`, and invokes `onJobComplete` with `success = false` and the error string

#### Scenario: Download succeeds
- **WHEN** both URL resolution and `DownloadAsync` return `Result.Ok`
- **THEN** the worker marks the job as `SyncJobState.Completed` and invokes `onJobComplete` with `success = true`

### Requirement: SyncService resolves conflict download without throwing
`SyncService.ApplyConflictOutcomeAsync` SHALL use `Match`/`Bind` on the `Result` returned by `IGraphService.GetDownloadUrlAsync`. On error it SHALL log the message and raise a progress event with `SyncState.Error`. It SHALL NOT throw.

#### Scenario: Conflict download URL missing
- **WHEN** `GetDownloadUrlAsync` returns `Result.Error` during conflict resolution
- **THEN** the error message is logged and `RaiseProgress` is called with `SyncState.Error` and the error message

#### Scenario: Conflict download succeeds
- **WHEN** `GetDownloadUrlAsync` returns `Result.Ok` and the download completes
- **THEN** the local file is overwritten with the remote version and no error is raised

### Requirement: AccountFilesViewModel does not throw on missing DriveId
`AccountFilesViewModel` SHALL NOT throw `InvalidOperationException` when `_driveId` is `Option.None`. When the `DriveId` is not available and an operation requires it, the method SHALL log a warning and return early.

#### Scenario: DriveId not yet loaded when operation is triggered
- **WHEN** an operation requiring `_driveId` executes while `_driveId` is `Option.None`
- **THEN** a warning is logged and the operation returns without throwing

#### Scenario: DriveId available
- **WHEN** `_driveId` is `Option.Some` and an operation is triggered
- **THEN** the operation proceeds using the `DriveId` value

### Requirement: Avalonia converter ConvertBack retains NotSupportedException
One-way Avalonia `IValueConverter` implementations SHALL continue to throw `NotSupportedException` in their `ConvertBack` method. This is required by the Avalonia binding contract for one-way converters.

#### Scenario: ConvertBack called on one-way converter
- **WHEN** `ConvertBack` is invoked on any of the sync-state or bool converters
- **THEN** `NotSupportedException` is thrown
