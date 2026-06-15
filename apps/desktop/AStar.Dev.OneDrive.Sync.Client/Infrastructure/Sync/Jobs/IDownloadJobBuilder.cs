using AStar.Dev.OneDrive.Sync.Client.Accounts;
using AStar.Dev.OneDrive.Sync.Client.Data.Entities;
using AStar.Dev.OneDrive.Sync.Client.Domain;

namespace AStar.Dev.OneDrive.Sync.Client.Infrastructure.Sync.Jobs;

/// <summary>
/// Builds a download or upload sync job from a single remote delta item,
/// applying conflict detection, rule filtering, and phantom-item registration.
/// </summary>
public interface IDownloadJobBuilder
{
    /// <summary>
    /// Processes a single <paramref name="item"/>: applies sync rule filtering, registers folders and
    /// phantom files via <see cref="ISyncedItemRegistrar"/>, detects conflicts, and returns the resulting
    /// job or <see langword="null"/> when no action is needed.
    /// </summary>
    Task<SyncJob?> BuildOneAsync(OneDriveAccount account, AccountSyncConfig syncConfig, DeltaItem item, IReadOnlyList<SyncRuleEntity> rules, Dictionary<string, SyncedItemEntity> syncedItems, Func<SyncConflict, Task> onConflict, CancellationToken ct);
}
