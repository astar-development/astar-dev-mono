# SyncJob Discriminated Union Refactor

## Motivation

`SyncJob` carries two nullable properties — `DownloadUrl` and `UploadedRemoteItemId` — that only apply to specific operation types. This primitive-obsession smell manifests as runtime `switch (job.Direction)` branches in `DownloadWorker`, `ActivityItemViewModel`, and `SyncJobExecutor`. It is possible to construct a `DownloadSyncJob` with no `DownloadUrl`, or an `UploadSyncJob` with no `UploadedRemoteItemId`, and the compiler accepts it.

Replacing the `SyncDirection` enum with a discriminated union of three `SyncJob`-derived records:

- makes impossible states unrepresentable
- moves direction-specific data to where it belongs
- replaces runtime enum-switch with compile-time type dispatch (pattern matching)
- aligns with the established `AuthError` DU pattern already in this codebase

> **Note:** The data clump refactor (`syncjob-dataclump-refactor.md`) is complete. `SyncJob` already uses grouped records — `RemoteItemRef Remote`, `SyncFileTarget Target`, `SyncFileMetadata Metadata`, `SyncJobStatus Status` — instead of 15 flat primitives. All signatures and examples below reflect this.

---

## Proposed Type Hierarchy

### Abstract base — `SyncJob.cs`

```csharp
namespace AStar.Dev.OneDrive.Sync.Client.Domain;

/// <summary>
/// Base type for all sync file operations queued by the sync engine.
/// Construct via <see cref="SyncJobFactory"/> — never use derived constructors directly.
/// </summary>
public abstract record SyncJob(RemoteItemRef Remote, SyncFileTarget Target, SyncFileMetadata Metadata, SyncJobStatus Status);

/// <summary>Download a remote file to the local path.</summary>
public sealed record DownloadSyncJob(RemoteItemRef Remote, SyncFileTarget Target, SyncFileMetadata Metadata, SyncJobStatus Status, string DownloadUrl)
    : SyncJob(Remote, Target, Metadata, Status);

/// <summary>Upload a local file to OneDrive.</summary>
public sealed record UploadSyncJob(RemoteItemRef Remote, SyncFileTarget Target, SyncFileMetadata Metadata, SyncJobStatus Status, string? UploadedRemoteItemId = null)
    : SyncJob(Remote, Target, Metadata, Status);

/// <summary>Delete a local file that no longer exists on OneDrive.</summary>
public sealed record DeleteSyncJob(RemoteItemRef Remote, SyncFileTarget Target, SyncFileMetadata Metadata, SyncJobStatus Status)
    : SyncJob(Remote, Target, Metadata, Status);
```

Key changes from current `SyncJob`:
- `Direction` property removed — the type itself is the direction
- `DownloadUrl` lifted from nullable on base to **required** on `DownloadSyncJob`
- `UploadedRemoteItemId` lifted from nullable on base to optional on `UploadSyncJob` only
- `DeleteSyncJob` carries no extra fields — clean
- Flat primitives replaced with grouped records — `Remote`, `Target`, `Metadata`, `Status` (data clump refactor already complete)

### Factory — `SyncJobFactory.cs`

`SyncJobStatusFactory.Create()` is called internally — callers never pass `Id` or `QueuedAt` directly.

```csharp
namespace AStar.Dev.OneDrive.Sync.Client.Domain;

/// <summary>Creates typed <see cref="SyncJob"/> instances with auto-generated identity fields.</summary>
public static class SyncJobFactory
{
    /// <inheritdoc cref="DownloadSyncJob"/>
    public static DownloadSyncJob CreateDownload(RemoteItemRef remote, SyncFileTarget target, SyncFileMetadata metadata, string downloadUrl)
        => new(remote, target, metadata, SyncJobStatusFactory.Create(), downloadUrl);

    /// <inheritdoc cref="UploadSyncJob"/>
    public static UploadSyncJob CreateUpload(RemoteItemRef remote, SyncFileTarget target, SyncFileMetadata metadata)
        => new(remote, target, metadata, SyncJobStatusFactory.Create());

    /// <inheritdoc cref="DeleteSyncJob"/>
    public static DeleteSyncJob CreateDelete(RemoteItemRef remote, SyncFileTarget target, SyncFileMetadata metadata)
        => new(remote, target, metadata, SyncJobStatusFactory.Create());
}
```

`DownloadUrl` is a required parameter on `CreateDownload` — callers that currently pass `null` will fail to compile, surfacing all incomplete constructions.

### Call-site pattern

Build each grouped record argument before the factory call:

```csharp
var remote = RemoteItemRefFactory.Create(account.Id.Id, string.Empty, item.Id);
var target = SyncFileTargetFactory.Create(localPath, item.RelativePath ?? item.Name);
var metadata = SyncFileMetadataFactory.Create(item.Size, item.LastModified ?? DateTimeOffset.MinValue);

SyncJobFactory.CreateDownload(remote, target, metadata, item.DownloadUrl);
SyncJobFactory.CreateUpload(remote, target, metadata);
SyncJobFactory.CreateDelete(remote, target, metadata);
```

---

## SyncJobState Advisory — Keep as Enum

**Recommendation: do not convert `SyncJobState` to a discriminated union.**

| Criterion | SyncDirection | SyncJobState |
|-----------|--------------|-------------|
| Cases carry different data? | Yes — `DownloadUrl`, `UploadedRemoteItemId` | No — all states have identical data |
| Drives polymorphic behaviour? | Yes — different code paths per direction | No — state is observed, not dispatched on |
| EF Core query pressure? | Low (one index, mapping only) | High — `WHERE State = X` is the primary queue query |
| State transitions? | N/A (immutable at creation) | Linear: Queued → InProgress → Completed/Failed/Skipped |
| Gain from DU? | High — nullables eliminated, type-safe | None — adds record-reconstruction overhead on every state update |

`SyncJobState` is lifecycle data: the same job changes state over time. Encoding that as derived records would require replacing the entire `SyncJob` instance on each `UpdateJobStateAsync` call, propagating the new instance back through active pipeline stages. The enum keeps state updates cheap and EF Core queries simple.

---

## Impact Assessment

All production classes that consume `SyncJob`, `SyncDirection`, or `SyncJobState`.

| File | Impact | Change Required |
|------|--------|-----------------|
| `Domain/SyncJob.cs` | **High** | Complete replacement — abstract base + 3 sealed records (grouped-record params already in place) |
| `Domain/SyncJobFactory.cs` | **High** | Redesign — `Create` → `CreateDownload` / `CreateUpload` / `CreateDelete`; each takes grouped records, calls `SyncJobStatusFactory.Create()` internally |
| `Infrastructure/Sync/DownloadWorker.cs` | **High** | `switch (job.Direction)` → `switch (job) { case DownloadSyncJob d: ... }`; access `d.DownloadUrl` directly (no null-check needed) |
| `Data/Repositories/SyncRepository.cs` | **High** | Entity mapping must derive direction from job type; extract `DownloadUrl` / `UploadedRemoteItemId` by type |
| `Infrastructure/Sync/SyncJobExecutor.cs` | **Medium** | Direction equality check → `is DownloadSyncJob` / `is UploadSyncJob` |
| `Activity/ActivityItemViewModel.cs` | **Medium** | Direction switch expression → type switch |
| `Infrastructure/Sync/RemoteFolderEnumerator.cs` | **Medium** | `SyncJobFactory.Create(remote, target, metadata, SyncDirection.Download, status, ...)` → `SyncJobFactory.CreateDownload(remote, target, metadata, downloadUrl)` |
| `Infrastructure/Sync/LocalChangeDetector.cs` | **Medium** | `SyncJobFactory.Create(remote, target, metadata, SyncDirection.Upload, status)` → `SyncJobFactory.CreateUpload(remote, target, metadata)` |
| `Data/Entities/SyncJobEntity.cs` | **Medium** | `Direction` property still needed for persistence; source now derived via mapping, not copied directly |
| `Infrastructure/Sync/ParallelDownloadPipeline.cs` | **Medium** | `job.Complete()` / `job.Fail(error)` already correct (via `SyncJobExtensions`); verify `with` on upcast `SyncJob` ref preserves concrete type |
| `Infrastructure/Sync/RemoteEnumerationResult.cs` | **Low** | Holds `IReadOnlyList<SyncJob>` — base type unchanged |
| `Infrastructure/Sync/ILocalChangeDetector.cs` | **Low** | Returns `IReadOnlyList<SyncJob>` — unchanged |
| `Infrastructure/Sync/ISyncJobExecutor.cs` | **Low** | Accepts `IReadOnlyList<SyncJob>` — unchanged |
| `Infrastructure/Sync/IParallelDownloadPipeline.cs` | **Low** | Accepts `IEnumerable<SyncJob>` — unchanged |
| `Infrastructure/Sync/JobCompletedEventArgs.cs` | **Low** | Holds `SyncJob Job` — unchanged |
| `Data/ModelBuilderExtensions.cs` | **Low** | Enum-to-int conversion for `SyncDirection` moves to mapping helper; `SyncJobState` unchanged |
| `Data/Entities/SyncedItemEntityFactory.cs` | **Low** | `CreateFromDownloadJob` / `CreateFromUploadJob` already named by direction; parameter types may narrow to `DownloadSyncJob` / `UploadSyncJob` |

---

## Migration Notes

### EF Core / Database

`SyncJobEntity.Direction` column stores `SyncDirection` as an integer today. The column must remain to avoid a migration. Two options:

**Option A (recommended):** Move `SyncDirection` enum to `Data/` namespace, mark `internal`. The entity still maps to int. The repository derives direction during mapping:

```csharp
Direction = job switch
{
    DownloadSyncJob => SyncDirection.Download,
    UploadSyncJob   => SyncDirection.Upload,
    DeleteSyncJob   => SyncDirection.Delete,
    _               => throw new UnreachableException()
}
```

**Option B:** Delete `SyncDirection` from Domain, store a discriminator string (`"Download"` / `"Upload"` / `"Delete"`) — requires a new migration and data backfill.

Option A is zero-migration and keeps the int index intact.

### `with` Expressions and Derived Types

C# `with` on a positional record returns the **same concrete type**. `DownloadSyncJob` stays `DownloadSyncJob` after:

```csharp
job with { UploadedRemoteItemId = uploadedId }
```

This is safe as long as `job` is the concrete type (not upcast to `SyncJob`). If `ParallelDownloadPipeline` holds the job as `SyncJob`, a cast is required before applying `with`. Audit all `with` usages.

State transitions use `SyncJobExtensions` (already implemented — no nested `with` needed at call sites):

```csharp
job.Complete()      // DownloadSyncJob stays DownloadSyncJob
job.Fail(error)     // concrete type preserved
success ? job.Complete() : job.Fail(error)
```

### Pattern Matching Replacement

Before:
```csharp
switch (job.Direction)
{
    case SyncDirection.Download: ...
    case SyncDirection.Upload:   ...
    case SyncDirection.Delete:   ...
}
```

After:
```csharp
switch (job)
{
    case DownloadSyncJob d: ... // d.DownloadUrl available without null-check
    case UploadSyncJob u:   ... // u.UploadedRemoteItemId available without null-check
    case DeleteSyncJob:     ...
}
```

The compiler enforces exhaustiveness when `SyncJob` is abstract and all derived types are sealed in the same assembly.

### Property access chains — unchanged

All property accesses on `SyncJob` use the grouped-record paths established by the data clump refactor:

| Property | Access |
|----------|--------|
| Remote identity | `job.Remote.AccountId`, `job.Remote.FolderId`, `job.Remote.RemoteItemId` |
| Local paths | `job.Target.LocalPath`, `job.Target.RelativePath` |
| File attributes | `job.Metadata.FileSize`, `job.Metadata.RemoteModified` |
| Job lifecycle | `job.Status.Id`, `job.Status.State`, `job.Status.QueuedAt`, `job.Status.CompletedAt`, `job.Status.ErrorMessage` |
| Direction-specific | `d.DownloadUrl` (on `DownloadSyncJob`), `u.UploadedRemoteItemId` (on `UploadSyncJob`) |

---

## Suggested Implementation Order

1. **Domain** — replace `SyncJob.cs` (abstract base + 3 sealed records using grouped-record params) and update `SyncJobFactory.cs` (`Create` → `CreateDownload` / `CreateUpload` / `CreateDelete`, each taking grouped records). Grouped records and `SyncJobExtensions` are already in `Domain/` — no new files needed. Commit failing build as TDD red.
2. **Factory call sites** — `RemoteFolderEnumerator`, `LocalChangeDetector`. Restore compile.
3. **Pipeline** — `DownloadWorker` switch, `ParallelDownloadPipeline` `with` expression audit, `SyncJobExecutor` direction checks.
4. **Data layer** — `SyncRepository` mapping, `SyncJobEntity` direction derivation, `ModelBuilderExtensions`.
5. **UI** — `ActivityItemViewModel` type switch.
6. **Cleanup** — relocate `SyncDirection` enum to `Data/` as `internal`; update `SyncedItemEntityFactory` parameter types if narrowing is worthwhile.
7. **Tests** — update all test builders and factory calls; add tests for each derived type.
