using AStar.Dev.Functional.Extensions;
using AStar.Dev.OneDrive.Sync.Client.Data.Entities;
using AStar.Dev.OneDrive.Sync.Client.Domain;

using Microsoft.EntityFrameworkCore;
using AccountId = AStar.Dev.OneDrive.Sync.Client.Data.Entities.AccountId;

namespace AStar.Dev.OneDrive.Sync.Client.Data.Repositories;

/// <summary>
/// Repository for managing sync jobs and conflicts in the database.
/// </summary>
/// <param name="dbFactory">The database context factory.</param>
public sealed class SyncRepository(IDbContextFactory<AppDbContext> dbFactory) : ISyncRepository
{
    /// <inheritdoc/>
    public async Task EnqueueJobsAsync(IEnumerable<SyncJob> jobs, CancellationToken cancellationToken = default)
    {
        await using var db = await dbFactory.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);

        var entities = jobs.Select(j => new SyncJobEntity
        {
            Id             = j.Status.Id,
            AccountId      = j.Remote.AccountId,
            FolderId       = j.Remote.FolderId,
            RemoteItemId   = j.Remote.RemoteItemId,
            RelativePath   = j.Target.RelativePath,
            LocalPath      = j.Target.LocalPath,
            Direction      = j switch
            {
                DownloadSyncJob => SyncDirection.Download,
                UploadSyncJob   => SyncDirection.Upload,
                DeleteSyncJob   => SyncDirection.Delete,
                _               => throw new System.Diagnostics.UnreachableException()
            },
            State          = j.Status.State,
            DownloadUrl    = (j as DownloadSyncJob)?.DownloadUrl ?? Option.None<string>(),
            FileSize       = j.Metadata.FileSize,
            RemoteModified = j.Metadata.RemoteModified,
            QueuedAt       = j.Status.QueuedAt
        });

        db.SyncJobs.AddRange(entities);
        _ = await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc/>
    public async Task<List<SyncJobEntity>> GetPendingJobsAsync(AccountId accountId, CancellationToken cancellationToken = default)
    {
        await using var db = await dbFactory.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);

        return await db.SyncJobs
          .Where(j => j.AccountId == accountId &&
                      j.State == SyncJobState.Queued)
          .OrderBy(j => j.QueuedAt)
          .ToListAsync(cancellationToken)
          .ConfigureAwait(false);
    }

    /// <inheritdoc/>
    public async Task UpdateJobStateAsync(Guid jobId, SyncJobState state, Option<string> stateError, CancellationToken cancellationToken = default)
    {
        await using var db = await dbFactory.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);

        _ = await db.SyncJobs
            .Where(j => j.Id == jobId)
            .ExecuteUpdateAsync(s => s
                .SetProperty(j => j.State, state)
                .SetProperty(j => j.ErrorMessage, stateError)
                .SetProperty(j => j.CompletedAt,
                    state is SyncJobState.Completed or SyncJobState.Failed or SyncJobState.Skipped
                        ? Option.Some(DateTimeOffset.UtcNow)
                        : Option.None<DateTimeOffset>()), cancellationToken)
            .ConfigureAwait(false);
    }

    /// <inheritdoc/>
    public async Task ClearCompletedJobsAsync(AccountId accountId, CancellationToken cancellationToken = default)
    {
        await using var db = await dbFactory.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);

        _ = await db.SyncJobs
          .Where(job => job.AccountId == accountId && job.State == SyncJobState.Completed)
          .ExecuteDeleteAsync(cancellationToken)
          .ConfigureAwait(false);
    }

    /// <inheritdoc/>
    public async Task AddConflictAsync(SyncConflict conflict, CancellationToken cancellationToken = default)
    {
        await using var db = await dbFactory.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);

        _ = db.SyncConflicts.Add(new SyncConflictEntity
        {
            Id             = conflict.Id,
            AccountId      = conflict.Remote.AccountId,
            FolderId       = conflict.Remote.FolderId,
            RemoteItemId   = conflict.Remote.RemoteItemId,
            RelativePath   = conflict.Target.RelativePath,
            LocalPath      = conflict.Target.LocalPath,
            LocalModified  = conflict.Snapshot.LocalModified,
            RemoteModified = conflict.Snapshot.RemoteModified,
            LocalSize      = conflict.Snapshot.LocalSize,
            RemoteSize     = conflict.Snapshot.RemoteSize,
            State          = conflict.State,
            DetectedAt     = conflict.DetectedAt
        });

        _ = await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc/>
    public async Task<List<SyncConflictEntity>> GetPendingConflictsAsync(AccountId accountId, CancellationToken cancellationToken = default)
    {
        await using var db = await dbFactory.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);

        return await db.SyncConflicts
          .Where(c => c.AccountId == accountId &&
                      c.State == ConflictState.Pending)
          .OrderBy(c => c.DetectedAt)
          .ToListAsync(cancellationToken)
          .ConfigureAwait(false);
    }

    /// <inheritdoc/>
    public async Task ResolveConflictAsync(Guid conflictId, ConflictPolicy resolution, CancellationToken cancellationToken = default)
    {
        await using var db = await dbFactory.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);

        _ = await db.SyncConflicts
            .Where(c => c.Id == conflictId)
            .ExecuteUpdateAsync(s => s
                .SetProperty(c => c.State, ConflictState.Resolved)
                .SetProperty(c => c.Resolution, Option.Some(resolution))
                .SetProperty(c => c.ResolvedAt, Option.Some(DateTimeOffset.UtcNow)), cancellationToken)
            .ConfigureAwait(false);
    }

    /// <inheritdoc/>
    public async Task<int> GetPendingConflictCountAsync(AccountId accountId, CancellationToken cancellationToken = default)
    {
        await using var db = await dbFactory.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);

        return await db.SyncConflicts
          .CountAsync(c => c.AccountId == accountId &&
                           c.State == ConflictState.Pending, cancellationToken)
          .ConfigureAwait(false);
    }
}
