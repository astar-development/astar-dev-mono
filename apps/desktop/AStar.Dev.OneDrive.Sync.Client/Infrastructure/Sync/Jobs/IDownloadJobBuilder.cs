using AStar.Dev.OneDrive.Sync.Client.Accounts;
using AStar.Dev.OneDrive.Sync.Client.Data.Entities;
using AStar.Dev.OneDrive.Sync.Client.Domain;

namespace AStar.Dev.OneDrive.Sync.Client.Infrastructure.Sync.Jobs;

/// <summary>
/// Builds download and upload sync jobs from a list of remote delta items,
/// applying conflict detection, rule filtering, and phantom-item registration.
/// </summary>
public interface IDownloadJobBuilder
{
    /// <summary>
    /// Processes <paramref name="items"/>, applies sync rule filtering, registers folders and phantom
    /// files via <see cref="ISyncedItemRegistrar"/>, detects conflicts, and returns the resulting jobs.
    /// </summary>
    Task<IReadOnlyList<SyncJob>> BuildAsync(OneDriveAccount account, AccountSyncConfig syncConfig, IReadOnlyList<DeltaItem> items, IReadOnlyList<SyncRuleEntity> rules, Dictionary<string, SyncedItemEntity> syncedItems, Func<SyncConflict, Task> onConflict, CancellationToken ct);
}
