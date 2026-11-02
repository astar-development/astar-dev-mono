using AStar.Dev.Infrastructure.AppDb.Entities;

namespace AStarDev.OneDriveSyncClient.Data.Repositories;

/// <summary>Persists completed transfers so recent sync throughput survives an application restart.</summary>
public interface ISyncThroughputRepository
{
    /// <summary>Records a completed transfer of <paramref name="bytes"/> for the account at <paramref name="completedAt"/>.</summary>
    Task RecordAsync(AccountId accountId, long bytes, DateTimeOffset completedAt, CancellationToken cancellationToken);

    /// <summary>Returns every sample, across all accounts, completed at or after <paramref name="since"/>.</summary>
    Task<IReadOnlyList<SyncThroughputSampleEntity>> GetSinceAsync(DateTimeOffset since, CancellationToken cancellationToken);

    /// <summary>Removes every sample completed before <paramref name="cutoff"/>.</summary>
    Task DeleteOlderThanAsync(DateTimeOffset cutoff, CancellationToken cancellationToken);
}
