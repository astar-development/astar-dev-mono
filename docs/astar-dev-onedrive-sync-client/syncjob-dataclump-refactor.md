# SyncJob — Data Clump Extraction Refactor

## Motivation

`SyncJob` currently has 15 positional parameters. Several are raw primitives that always travel together and answer the same conceptual question. These are textbook **data clumps**: groups of values that appear as a unit at construction, mapping, and consumption sites but are scattered as individual primitives.

Extracting them into named records:

- reduces parameter count (15 → 7)
- names the concept, not just the values
- reveals intent at construction and read sites
- makes `SyncJobFactory.Create` signature shorter; call sites migrate to per-concept factory methods, making construction self-documenting
- replaces verbose nested `with` state updates with named transition extension methods (`Complete()`, `Fail()`)

This is independent of — and complementary to — the `SyncDirection` discriminated union refactor described in `syncjob-refactor.md`.

---

## Identified Data Clumps

### Clump 1 — Remote file identity

**Fields:** `AccountId`, `FolderId`, `RemoteItemId`

**Evidence:**
- `EnqueueJobsAsync` maps all three to entity in the same block
- `ExecuteJobAsync` uses `FolderId` for Graph API upload call
- `SyncedItemEntityFactory.CreateFromDownloadJob` reads `RemoteItemId`
- `SyncedItemEntityFactory.CreateFromUploadJob` reads `LocalPath` alongside `RemoteItemId`

**Question answered:** *Which remote item are we syncing?*

---

### Clump 2 — Local file target

**Fields:** `LocalPath`, `RelativePath`

**Evidence:**
- `EnqueueJobsAsync` maps both together
- `ExecuteJobAsync` uses `LocalPath` for download/delete/upload; `RelativePath` for logging and as upload path fallback
- `ActivityItemViewModel.FromJob` reads both (`RelativePath` for display, `Path.GetFileName(RelativePath)`)
- `SyncedItemEntityFactory` uses `LocalPath` in both download and upload factory methods

**Question answered:** *Where does this file live locally?*

---

### Clump 3 — Remote file attributes

**Fields:** `FileSize`, `RemoteModified`

**Evidence:**
- `EnqueueJobsAsync` maps both together
- `ExecuteJobAsync` passes `RemoteModified` to the file downloader (for timestamp preservation)
- `SyncedItemEntityFactory.CreateFromDownloadJob` reads `RemoteModified`
- `ActivityItemViewModel.FromJob` reads `FileSize`

**Question answered:** *What are the remote file's current attributes?*

---

### Clump 4 — Job lifecycle status *(optional — see tradeoff below)*

**Fields:** `Id`, `QueuedAt`, `State`, `ErrorMessage?`, `CompletedAt?`

**Evidence:**
- `EnqueueJobsAsync` maps `Id`, `State`, `QueuedAt` together
- `RunAsync` passes `job.Id` to every `UpdateJobStateAsync` call
- `ActivityItemViewModel.FromJob` reads `CompletedAt` and `ErrorMessage` together
- `ParallelDownloadPipeline` applies `with { State = ..., CompletedAt = ... }` together

**Question answered:** *What is this job's identity and current execution state?*

**Tradeoff addressed via extension methods:** Naively, state updates become nested `with`:
```csharp
job with { Status = job.Status with { State = SyncJobState.Completed, CompletedAt = DateTimeOffset.UtcNow } }
```
This is eliminated by `SyncJobExtensions` — see section below. Call sites reduce to:
```csharp
job.Complete()
job.Fail(error)
success ? job.Complete() : job.Fail(error)
```

---

## Proposed New Records

All files placed in `Domain/`. Each record ships with its factory class per repo convention.

### `RemoteItemRef.cs`

```csharp
namespace AStar.Dev.OneDrive.Sync.Client.Domain;

/// <summary>Identifies a specific item in a OneDrive drive folder.</summary>
public sealed record RemoteItemRef(string AccountId, string FolderId, string RemoteItemId);
```

### `RemoteItemRefFactory.cs`

```csharp
namespace AStar.Dev.OneDrive.Sync.Client.Domain;

/// <summary>Factory for <see cref="RemoteItemRef"/>.</summary>
public static class RemoteItemRefFactory
{
    /// <summary>Creates a <see cref="RemoteItemRef"/> identifying a specific remote drive item.</summary>
    public static RemoteItemRef Create(string accountId, string folderId, string remoteItemId)
        => new(accountId, folderId, remoteItemId);
}
```

---

### `SyncFileTarget.cs`

```csharp
namespace AStar.Dev.OneDrive.Sync.Client.Domain;

/// <summary>The local paths involved in a sync operation.</summary>
public sealed record SyncFileTarget(string LocalPath, string RelativePath);
```

### `SyncFileTargetFactory.cs`

```csharp
namespace AStar.Dev.OneDrive.Sync.Client.Domain;

/// <summary>Factory for <see cref="SyncFileTarget"/>.</summary>
public static class SyncFileTargetFactory
{
    /// <summary>Creates a <see cref="SyncFileTarget"/> for the given local paths.</summary>
    public static SyncFileTarget Create(string localPath, string relativePath)
        => new(localPath, relativePath);
}
```

---

### `SyncFileMetadata.cs`

```csharp
namespace AStar.Dev.OneDrive.Sync.Client.Domain;

/// <summary>Remote file attributes captured at the time the sync job was queued.</summary>
public sealed record SyncFileMetadata(long FileSize, DateTimeOffset RemoteModified);
```

### `SyncFileMetadataFactory.cs`

```csharp
namespace AStar.Dev.OneDrive.Sync.Client.Domain;

/// <summary>Factory for <see cref="SyncFileMetadata"/>.</summary>
public static class SyncFileMetadataFactory
{
    /// <summary>Creates a <see cref="SyncFileMetadata"/> from the given file attributes.</summary>
    public static SyncFileMetadata Create(long fileSize, DateTimeOffset remoteModified)
        => new(fileSize, remoteModified);
}
```

---

### `SyncJobStatus.cs` *(Clump 4)*

```csharp
namespace AStar.Dev.OneDrive.Sync.Client.Domain;

/// <summary>Identity and execution state of a sync job.</summary>
public sealed record SyncJobStatus(Guid Id, DateTimeOffset QueuedAt, SyncJobState State = SyncJobState.Queued, string? ErrorMessage = null, DateTimeOffset? CompletedAt = null);
```

### `SyncJobStatusFactory.cs`

```csharp
namespace AStar.Dev.OneDrive.Sync.Client.Domain;

/// <summary>Factory for <see cref="SyncJobStatus"/>.</summary>
public static class SyncJobStatusFactory
{
    /// <summary>Creates a new <see cref="SyncJobStatus"/> with a generated <see cref="SyncJobStatus.Id"/> and <see cref="SyncJobStatus.QueuedAt"/> timestamp.</summary>
    public static SyncJobStatus Create() => new(Guid.NewGuid(), DateTimeOffset.UtcNow);
}
```

---

### `SyncJobExtensions.cs`

Extension methods on `SyncJob` for state transitions. Eliminates nested `with` at every call site; names the intent, not the mechanism.

```csharp
namespace AStar.Dev.OneDrive.Sync.Client.Domain;

/// <summary>State-transition extensions for <see cref="SyncJob"/>.</summary>
public static class SyncJobExtensions
{
    /// <summary>Returns a copy of <paramref name="job"/> transitioned to <see cref="SyncJobState.Completed"/>.</summary>
    public static SyncJob Complete(this SyncJob job) => job with { Status = job.Status with { State = SyncJobState.Completed, CompletedAt = DateTimeOffset.UtcNow } };

    /// <summary>Returns a copy of <paramref name="job"/> transitioned to <see cref="SyncJobState.Failed"/>.</summary>
    public static SyncJob Fail(this SyncJob job, string? errorMessage = null) => job with { Status = job.Status with { State = SyncJobState.Failed, ErrorMessage = errorMessage, CompletedAt = DateTimeOffset.UtcNow } };
}
```

---

## Updated `SyncJob.cs`

```csharp
namespace AStar.Dev.OneDrive.Sync.Client.Domain;

/// <summary>
/// Represents a single file operation queued by the sync engine.
/// Created from delta query results and processed in order.
/// Construct via <see cref="SyncJobFactory"/>.
/// </summary>
public sealed record SyncJob(
    RemoteItemRef Remote,
    SyncFileTarget Target,
    SyncFileMetadata Metadata,
    SyncDirection Direction,
    SyncJobStatus Status,
    string? DownloadUrl = null,
    string? UploadedRemoteItemId = null);
```

**15 parameters → 7.** `Direction`, `DownloadUrl`, and `UploadedRemoteItemId` remain flat; their nullable smell is addressed separately in `syncjob-refactor.md`.

---

## Updated `SyncJobFactory.cs`

`SyncJobFactory.Create` takes the new record objects directly. Call sites **must** construct each argument via the relevant factory `Create` method — no primitive threading through `SyncJobFactory`.

```csharp
namespace AStar.Dev.OneDrive.Sync.Client.Domain;

/// <summary>Creates <see cref="SyncJob"/> instances.</summary>
public static class SyncJobFactory
{
    /// <summary>Creates a new <see cref="SyncJob"/> from pre-constructed domain objects.</summary>
    public static SyncJob Create(RemoteItemRef remote, SyncFileTarget target, SyncFileMetadata metadata, SyncDirection direction, SyncJobStatus status, string? downloadUrl = null, string? uploadedRemoteItemId = null)
        => new(remote, target, metadata, direction, status, downloadUrl, uploadedRemoteItemId);
}
```

### Call-site pattern

Build each argument explicitly before the final factory call:

```csharp
var remote = RemoteItemRefFactory.Create(account.Id.Id, string.Empty, item.Id);
var target = SyncFileTargetFactory.Create(localPath, item.RelativePath ?? item.Name);
var metadata = SyncFileMetadataFactory.Create(item.Size, item.LastModified ?? DateTimeOffset.MinValue);
var status = SyncJobStatusFactory.Create();

SyncJobFactory.Create(remote, target, metadata, SyncDirection.Download, status, downloadUrl: item.DownloadUrl);
```

---

## Consumer Migration Guide

### Property access chains

| Before | After |
|--------|-------|
| `job.AccountId` | `job.Remote.AccountId` |
| `job.FolderId` | `job.Remote.FolderId` |
| `job.RemoteItemId` | `job.Remote.RemoteItemId` |
| `job.LocalPath` | `job.Target.LocalPath` |
| `job.RelativePath` | `job.Target.RelativePath` |
| `job.FileSize` | `job.Metadata.FileSize` |
| `job.RemoteModified` | `job.Metadata.RemoteModified` |
| `job.Id` | `job.Status.Id` |
| `job.QueuedAt` | `job.Status.QueuedAt` |
| `job.State` | `job.Status.State` |
| `job.ErrorMessage` | `job.Status.ErrorMessage` |
| `job.CompletedAt` | `job.Status.CompletedAt` |
| `job.Direction` | `job.Direction` *(unchanged)* |
| `job.DownloadUrl` | `job.DownloadUrl` *(unchanged)* |
| `job.UploadedRemoteItemId` | `job.UploadedRemoteItemId` *(unchanged)* |

### `with` expression migration

Single-field update — no change in form:
```csharp
// before
job with { UploadedRemoteItemId = uploadedId }

// after — unchanged (field is still on SyncJob)
job with { UploadedRemoteItemId = uploadedId }
```

Status transitions — use `SyncJobExtensions`:
```csharp
// before — completed
job with { State = SyncJobState.Completed, CompletedAt = DateTimeOffset.UtcNow }
// after
job.Complete()

// before — failed
job with { State = SyncJobState.Failed, ErrorMessage = error, CompletedAt = DateTimeOffset.UtcNow }
// after
job.Fail(error)

// before — conditional (ParallelDownloadPipeline pattern)
job with { State = success ? SyncJobState.Completed : SyncJobState.Failed, ErrorMessage = error, CompletedAt = DateTimeOffset.UtcNow }
// after
success ? job.Complete() : job.Fail(error)
```

### Files requiring changes

All files affected are internal to the desktop client project.

| File | Change required |
|------|-----------------|
| `Domain/SyncJob.cs` | Replace with grouped record |
| `Domain/SyncJobFactory.cs` | New signature — accepts record objects, not primitives |
| `Domain/SyncJobExtensions.cs` | New file — `Complete()` and `Fail(error?)` transition extension methods |
| `Infrastructure/Sync/LocalChangeDetector.cs` | `SyncJobFactory.Create(accountId, ...)` → build args via sub-factories |
| `Infrastructure/Sync/RemoteFolderEnumerator.cs` | `SyncJobFactory.Create(account.Id.Id, ...)` × 4 calls → build args via sub-factories |
| `Data/Repositories/SyncRepository.cs` | `j.AccountId` → `j.Remote.AccountId` etc. (entity mapping block) |
| `Data/Entities/SyncedItemEntityFactory.cs` | `job.RemoteItemId` → `job.Remote.RemoteItemId`; `job.LocalPath` → `job.Target.LocalPath`; `job.RemoteModified` → `job.Metadata.RemoteModified` |
| `Infrastructure/Sync/DownloadWorker.cs` | `job.LocalPath` → `job.Target.LocalPath`; `job.RelativePath` → `job.Target.RelativePath`; `job.RemoteModified` → `job.Metadata.RemoteModified`; `job.FolderId` → `job.Remote.FolderId`; `job.Id` → `job.Status.Id` |
| `Activity/ActivityItemViewModel.cs` | `job.AccountId` → `job.Remote.AccountId`; `job.RelativePath` → `job.Target.RelativePath`; `job.FileSize` → `job.Metadata.FileSize`; `job.CompletedAt` → `job.Status.CompletedAt`; `job.ErrorMessage` → `job.Status.ErrorMessage` |
| `Infrastructure/Sync/ParallelDownloadPipeline.cs` | `with { State = ..., CompletedAt = ... }` → nested `Status with { ... }` |
| `Infrastructure/Sync/SyncJobExecutor.cs` | `args.Job.State` → `args.Job.Status.State` |
| Test files (4 files) | `MakeJob` / `BuildSyncJob` / `MinimalJob` helpers — update both call sites and property access chains |

---

## Implementation Order

1. Add new record and factory files (`RemoteItemRef`, `SyncFileTarget`, `SyncFileMetadata`, `SyncJobStatus` and their factories) — compiles immediately alongside old `SyncJob`.
2. Add `SyncJobExtensions.cs` — compiles immediately (references only `SyncJob` which still exists).
3. Replace `SyncJob.cs` with the 7-parameter version and update `SyncJobFactory.cs` with new record-object signature — project fails to build.
4. Fix `LocalChangeDetector` — replace flat `SyncJobFactory.Create` call with sub-factory arguments.
5. Fix `RemoteFolderEnumerator` — replace 4 flat `SyncJobFactory.Create` calls with sub-factory arguments.
6. Fix `SyncRepository.EnqueueJobsAsync` mapping block.
7. Fix `DownloadWorker` property accesses.
8. Fix `SyncJobExecutor` (`args.Job.Status.State`) and `ParallelDownloadPipeline` (`success ? job.Complete() : job.Fail(error)`).
9. Fix `ActivityItemViewModel.FromJob`.
10. Fix `SyncedItemEntityFactory`.
11. Fix test helper methods (`MakeJob`, `BuildSyncJob`, `MinimalJob`, `CreateMinimalJob`) — update both call sites and property access chains.
12. `dotnet build` — zero errors, zero warnings.
13. `dotnet test` — all existing tests pass.
