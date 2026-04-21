using AStar.Dev.OneDrive.Sync.Client.Data.Entities;
using AStar.Dev.OneDrive.Sync.Client.Domain;
using AStar.Dev.OneDrive.Sync.Client.Models;
using Microsoft.EntityFrameworkCore;

namespace AStar.Dev.OneDrive.Sync.Client.Data.Repositories;

public sealed class SyncRepository(IDbContextFactory<AppDbContext> dbFactory) : ISyncRepository
{
    public async Task EnqueueJobsAsync(IEnumerable<SyncJob> jobs)
    {
        await using var db = await dbFactory.CreateDbContextAsync();

        var entities = jobs.Select(j => new SyncJobEntity
        {
            Id             = j.Id,
            AccountId      = new AccountId(j.AccountId),
            FolderId       = new OneDriveFolderId(j.FolderId),
            RemoteItemId   = new OneDriveItemId(j.RemoteItemId),
            RelativePath   = j.RelativePath,
            LocalPath      = j.LocalPath,
            Direction      = j.Direction,
            State          = j.State,
            DownloadUrl    = j.DownloadUrl,
            FileSize       = j.FileSize,
            RemoteModified = j.RemoteModified,
            QueuedAt       = j.QueuedAt
        });

        db.SyncJobs.AddRange(entities);
        _ = await db.SaveChangesAsync();
    }

    public async Task<List<SyncJobEntity>> GetPendingJobsAsync(AccountId accountId)
    {
        await using var db = await dbFactory.CreateDbContextAsync();

        return await db.SyncJobs
          .Where(j => j.AccountId == accountId &&
                      j.State == SyncJobState.Queued)
          .OrderBy(j => j.QueuedAt)
          .ToListAsync();
    }

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

    public async Task ClearCompletedJobsAsync(AccountId accountId)
    {
        await using var db = await dbFactory.CreateDbContextAsync();

        _ = await db.SyncJobs
          .Where(job => job.AccountId == accountId && job.State == SyncJobState.Completed)
          .ExecuteDeleteAsync();
    }

    public async Task AddConflictAsync(SyncConflict conflict)
    {
        await using var db = await dbFactory.CreateDbContextAsync();

        _ = db.SyncConflicts.Add(new SyncConflictEntity
        {
            Id             = conflict.Id,
            AccountId      = new AccountId(conflict.AccountId),
            FolderId       = new OneDriveFolderId(conflict.FolderId),
            RemoteItemId   = new OneDriveItemId(conflict.RemoteItemId),
            RelativePath   = conflict.RelativePath,
            LocalPath      = conflict.LocalPath,
            LocalModified  = conflict.LocalModified,
            RemoteModified = conflict.RemoteModified,
            LocalSize      = conflict.LocalSize,
            RemoteSize     = conflict.RemoteSize,
            State          = conflict.State,
            DetectedAt     = conflict.DetectedAt
        });

        _ = await db.SaveChangesAsync();
    }

    public async Task<List<SyncConflictEntity>> GetPendingConflictsAsync(AccountId accountId)
    {
        await using var db = await dbFactory.CreateDbContextAsync();

        return await db.SyncConflicts
          .Where(c => c.AccountId == accountId &&
                      c.State == ConflictState.Pending)
          .OrderBy(c => c.DetectedAt)
          .ToListAsync();
    }

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

    public async Task<int> GetPendingConflictCountAsync(AccountId accountId)
    {
        await using var db = await dbFactory.CreateDbContextAsync();

        return await db.SyncConflicts
          .CountAsync(c => c.AccountId == accountId &&
                           c.State == ConflictState.Pending);
    }
}
