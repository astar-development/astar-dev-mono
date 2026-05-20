using AStar.Dev.OneDrive.Sync.Client.Data.Entities;
using AStar.Dev.OneDrive.Sync.Client.Domain;

namespace AStar.Dev.OneDrive.Sync.Client.Infrastructure.Sync.Detection;

public interface ILocalChangeDetector
{
    /// <summary>
    /// Scans local directories matching the supplied inclusion rules and returns upload jobs
    /// for files that are new (not in <paramref name="syncedItemsByLocalPath"/>) or modified since they were last synced.
    /// </summary>
    IReadOnlyList<SyncJob> DetectNewAndModifiedFiles(string accountId, string localBasePath, IReadOnlyList<SyncRuleEntity> rules, IReadOnlyDictionary<string, SyncedItemEntity> syncedItemsByLocalPath);
}
