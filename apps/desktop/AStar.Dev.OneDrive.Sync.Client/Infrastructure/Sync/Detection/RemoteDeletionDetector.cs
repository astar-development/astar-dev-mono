using System.IO.Abstractions;
using AStar.Dev.OneDrive.Sync.Client.Data.Entities;
using AStar.Dev.OneDrive.Sync.Client.Data.Repositories;
using AStar.Dev.OneDrive.Sync.Client.Infrastructure.Logging;
using Microsoft.Extensions.Logging;
using AccountId = AStar.Dev.OneDrive.Sync.Client.Data.Entities.AccountId;

namespace AStar.Dev.OneDrive.Sync.Client.Infrastructure.Sync.Detection;

/// <inheritdoc />
public sealed class RemoteDeletionDetector(ISyncedItemRepository syncedItemRepository, IFileSystem fileSystem, ILogger<RemoteDeletionDetector> logger) : IRemoteDeletionDetector
{
    /// <inheritdoc />
    public async Task DetectAndApplyAsync(AccountId accountId, Dictionary<string, SyncedItemEntity> syncedItems, IReadOnlySet<string> seenRemoteIds, IReadOnlyList<SyncRuleEntity> rules, CancellationToken ct)
    {
        List<OneDriveItemId> deletedRemoteIds = [];

        foreach (var (remoteId, knownItem) in syncedItems.ToList())
        {
            if (ct.IsCancellationRequested)
                break;

            if (!SyncRuleEvaluator.IsIncluded(knownItem.RemotePath, rules))
                continue;

            if (seenRemoteIds.Contains(remoteId))
                continue;

            OneDriveSyncClientMessages.RemoteDeletionDetectorNotPresent(logger, knownItem.RemotePath);
            DeleteLocalItem(knownItem);
            deletedRemoteIds.Add(knownItem.RemoteItemId);
        }

        if (deletedRemoteIds.Count == 0)
            return;

        await syncedItemRepository.DeleteManyByRemoteIdAsync(accountId, deletedRemoteIds, ct).ConfigureAwait(false);

        foreach (var remoteId in deletedRemoteIds)
            syncedItems.Remove(remoteId.Id);
    }

    private void DeleteLocalItem(SyncedItemEntity knownItem)
    {
        string localPath = knownItem.LocalPath;

        if (knownItem.IsFolder)
        {
            if (fileSystem.Directory.Exists(localPath))
            {
                OneDriveSyncClientMessages.RemoteDeletionDetectorFolderDeleted(logger, localPath);
                fileSystem.Directory.Delete(localPath, recursive: true);
            }
        }
        else
        {
            if (fileSystem.File.Exists(localPath))
            {
                OneDriveSyncClientMessages.RemoteDeletionDetectorFileDeleted(logger, localPath);
                fileSystem.File.Delete(localPath);
            }
        }
    }
}
