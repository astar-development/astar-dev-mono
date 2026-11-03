using AStar.Dev.Infrastructure.AppDb;
using AStar.Dev.Infrastructure.AppDb.Entities;
using Microsoft.EntityFrameworkCore;

namespace AStarDev.OneDriveSyncClient.Data.Repositories;

/// <inheritdoc />
public sealed class SyncThroughputRepository(IDbContextFactory<AppDbContext> dbFactory) : ISyncThroughputRepository
{
    /// <inheritdoc />
    public async Task RecordAsync(AccountId accountId, long bytes, DateTimeOffset completedAt, CancellationToken cancellationToken)
    {
        await using var db = await dbFactory.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);

        _ = db.SyncThroughputSamples.Add(new SyncThroughputSampleEntity { AccountId = accountId, Bytes = bytes, CompletedAt = completedAt });
        _ = await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<SyncThroughputSampleEntity>> GetSinceAsync(DateTimeOffset since, CancellationToken cancellationToken)
    {
        await using var db = await dbFactory.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);

        return await db.SyncThroughputSamples.AsNoTracking().Where(sample => sample.CompletedAt >= since).ToListAsync(cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task DeleteOlderThanAsync(DateTimeOffset cutoff, CancellationToken cancellationToken)
    {
        await using var db = await dbFactory.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);

        _ = await db.SyncThroughputSamples.Where(sample => sample.CompletedAt < cutoff).ExecuteDeleteAsync(cancellationToken).ConfigureAwait(false);
    }
}
