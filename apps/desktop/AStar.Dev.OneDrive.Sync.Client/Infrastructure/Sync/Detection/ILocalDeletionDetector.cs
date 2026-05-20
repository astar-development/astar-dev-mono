using AStar.Dev.OneDrive.Sync.Client.Data.Entities;
using AccountId = AStar.Dev.OneDrive.Sync.Client.Data.Entities.AccountId;

namespace AStar.Dev.OneDrive.Sync.Client.Infrastructure.Sync.Detection;

/// <summary>
/// Detects locally deleted files and propagates those deletions to the remote drive.
/// </summary>
public interface ILocalDeletionDetector
{
    /// <summary>
    /// Walks <paramref name="syncedItems"/> and, for each file no longer present on disk,
    /// deletes the corresponding remote item via Graph and removes the local tracking record.
    /// </summary>
    Task DetectAndApplyAsync(AccountId accountId, string accessToken, Dictionary<string, SyncedItemEntity> syncedItems, CancellationToken ct);
}
