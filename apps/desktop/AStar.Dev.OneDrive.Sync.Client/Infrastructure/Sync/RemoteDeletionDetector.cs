using System.IO.Abstractions;
using AStar.Dev.OneDrive.Sync.Client.Data.Entities;
using AStar.Dev.OneDrive.Sync.Client.Data.Repositories;
using AStar.Dev.OneDrive.Sync.Client.Domain;
using AStar.Dev.OneDrive.Sync.Client.Models;

namespace AStar.Dev.OneDrive.Sync.Client.Infrastructure.Sync;

/// <inheritdoc />
public sealed class RemoteDeletionDetector(ISyncedItemRepository syncedItemRepository, IFileSystem fileSystem) : IRemoteDeletionDetector
{
    /// <inheritdoc />
    public async Task DetectAndApplyAsync(AccountId accountId, Dictionary<string, SyncedItemEntity> syncedItems, IReadOnlySet<string> seenRemoteIds, IReadOnlyList<SyncRuleEntity> rules, CancellationToken ct)
    {
        foreach(var (remoteId, knownItem) in syncedItems.ToList())
        {
            if(ct.IsCancellationRequested)
                break;

            if(!SyncRuleEvaluator.IsIncluded(knownItem.RemotePath, rules))
                continue;

            if(seenRemoteIds.Contains(remoteId))
                continue;

            Serilog.Log.Information("[RemoteDeletionDetector] Remote item no longer present — treating as deleted: {Path}", knownItem.RemotePath);
            await HandleRemoteDeleteAsync(accountId, knownItem, syncedItems, ct);
        }
    }

    private async Task HandleRemoteDeleteAsync(AccountId accountId, SyncedItemEntity knownItem, Dictionary<string, SyncedItemEntity> syncedItems, CancellationToken ct)
    {
        string localPath = knownItem.LocalPath;

        if(knownItem.IsFolder)
        {
            if(fileSystem.Directory.Exists(localPath))
            {
                Serilog.Log.Information("[RemoteDeletionDetector] Remote folder deleted — removing local: {Path}", localPath);
                fileSystem.Directory.Delete(localPath, recursive: true);
            }
        }
        else
        {
            if(fileSystem.File.Exists(localPath))
            {
                Serilog.Log.Information("[RemoteDeletionDetector] Remote file deleted — removing local: {Path}", localPath);
                fileSystem.File.Delete(localPath);
            }
        }

        await syncedItemRepository.DeleteByRemoteIdAsync(accountId, knownItem.RemoteItemId, ct);
        syncedItems.Remove(knownItem.RemoteItemId.Id);
    }
}
