using AStar.Dev.OneDrive.Sync.Client.Data.Entities;
using AStar.Dev.OneDrive.Sync.Client.Models;

namespace AStar.Dev.OneDrive.Sync.Client.Infrastructure.Sync;

public interface ILocalChangeDetector
{
    /// <summary>
    /// Scans local directories matching the supplied inclusion rules and returns upload jobs
    /// for files that are new (not in <paramref name="localPathLookup"/>) or modified since they were last synced.
    /// </summary>
    List<SyncJob> DetectNewAndModifiedFiles(string accountId, string localBasePath, IReadOnlyList<SyncRuleEntity> rules, IReadOnlyDictionary<string, SyncedItemEntity> localPathLookup);
}
