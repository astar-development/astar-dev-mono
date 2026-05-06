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
    public async Task EnqueueJobsAsync(IEnumerable<SyncJob> jobs)
    {
        await using var db = await dbFactory.CreateDbContextAsync();

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
            DownloadUrl    = (j as DownloadSyncJob)?.DownloadUrl,
            FileSize       = j.Metadata.FileSize,
            RemoteModified = j.Metadata.RemoteModified,
            QueuedAt       = j.Status.QueuedAt
        });

        db.SyncJobs.AddRange(entities);
        _ = await db.SaveChangesAsync();
    }

    /// <inheritdoc/>
    public async Task<List<SyncJobEntity>> GetPendingJobsAsync(AccountId accountId)
    {
        await using var db = await dbFactory.CreateDbContextAsync();

        return await db.SyncJobs
          .Where(j => j.AccountId == accountId &&
                      j.State == SyncJobState.Queued)
          .OrderBy(j => j.QueuedAt)
          .ToListAsync();
    }

    /// <inheritdoc/>
    public async Task UpdateJobStateAsync(Guid jobId, SyncJobState state, string? stateError = null)
    {
        await using var db = await dbFactory.CreateDbContextAsync();

        _ = await db.SyncJobs
            .Where(j => j.Id == jobId)
            .ExecuteUpdateAsync(s => s
                .SetProperty(j => j.State, state)
                .SetProperty(j => j.ErrorMessage, stateError)
                .SetProperty(j => j.CompletedAt,
                    state is SyncJobState.Completed or SyncJobState.Failed or SyncJobState.Skipped
                        ? DateTimeOffset.UtcNow
                        : null));
    }

    /// <inheritdoc/>
    public async Task ClearCompletedJobsAsync(AccountId accountId)
    {
        await using var db = await dbFactory.CreateDbContextAsync();

        _ = await db.SyncJobs
          .Where(job => job.AccountId == accountId && job.State == SyncJobState.Completed)
          .ExecuteDeleteAsync();
    }

    /// <inheritdoc/>
    public async Task AddConflictAsync(SyncConflict conflict)
    {
        await using var db = await dbFactory.CreateDbContextAsync();

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

        _ = await db.SaveChangesAsync();
    }

    /// <inheritdoc/>
    public async Task<List<SyncConflictEntity>> GetPendingConflictsAsync(AccountId accountId)
    {
        await using var db = await dbFactory.CreateDbContextAsync();

        return await db.SyncConflicts
          .Where(c => c.AccountId == accountId &&
                      c.State == ConflictState.Pending)
          .OrderBy(c => c.DetectedAt)
          .ToListAsync();
    }

    /// <inheritdoc/>
    public async Task ResolveConflictAsync(Guid conflictId, ConflictPolicy resolution)
    {
        await using var db = await dbFactory.CreateDbContextAsync();

        _ = await db.SyncConflicts
            .Where(c => c.Id == conflictId)
            .ExecuteUpdateAsync(s => s
                .SetProperty(c => c.State, ConflictState.Resolved)
                .SetProperty(c => c.Resolution, resolution)
                .SetProperty(c => c.ResolvedAt, DateTimeOffset.UtcNow));
    }

    /// <inheritdoc/>
    public async Task<int> GetPendingConflictCountAsync(AccountId accountId)
    {
        await using var db = await dbFactory.CreateDbContextAsync();

        return await db.SyncConflicts
          .CountAsync(c => c.AccountId == accountId &&
                           c.State == ConflictState.Pending);
    }
}
