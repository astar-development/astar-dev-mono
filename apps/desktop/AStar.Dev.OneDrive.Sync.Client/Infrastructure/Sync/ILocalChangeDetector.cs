using AStar.Dev.OneDrive.Sync.Client.Models;

namespace AStar.Dev.OneDrive.Sync.Client.Infrastructure.Sync;

public interface ILocalChangeDetector
{
    /// <summary>
    /// Returns upload jobs for all local files in <paramref name="localFolderPath"/>
    /// that are newer than <paramref name="since"/>.
    /// Pass null for <paramref name="since"/> to queue everything (first upload pass).
    /// </summary>
#pragma warning disable CA1822 // Method is called from another class, not sure why that call is not detected.
    List<SyncJob> DetectChanges(string accountId, string folderId, string localFolderPath, string remoteFolderPath, DateTimeOffset? since)
#pragma warning restore CA1822
        ;
}
