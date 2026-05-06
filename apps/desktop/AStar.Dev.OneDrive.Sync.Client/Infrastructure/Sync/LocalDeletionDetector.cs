using System.IO.Abstractions;
using AStar.Dev.OneDrive.Sync.Client.Data.Entities;
using AStar.Dev.OneDrive.Sync.Client.Data.Repositories;
using AStar.Dev.OneDrive.Sync.Client.Infrastructure.Graph;
using AccountId = AStar.Dev.OneDrive.Sync.Client.Data.Entities.AccountId;

namespace AStar.Dev.OneDrive.Sync.Client.Infrastructure.Sync;

/// <inheritdoc />
public sealed class LocalDeletionDetector(IGraphService graphService, ISyncedItemRepository syncedItemRepository, IFileSystem fileSystem) : ILocalDeletionDetector
{
    /// <inheritdoc />
    public async Task DetectAndApplyAsync(AccountId accountId, string accessToken, Dictionary<string, SyncedItemEntity> syncedItems, CancellationToken ct)
    {
        foreach(var (remoteId, knownItem) in syncedItems)
        {
            if(knownItem.IsFolder) continue;
            if(ct.IsCancellationRequested) break;
            if(fileSystem.File.Exists(knownItem.LocalPath)) continue;

            Serilog.Log.Information("[LocalDeletionDetector] Local file deleted — removing remote: {Path}", knownItem.RemotePath);

            try
            {
                await graphService.DeleteItemAsync(accessToken, remoteId, ct);
                await syncedItemRepository.DeleteByRemoteIdAsync(accountId, knownItem.RemoteItemId, ct);
            }
            catch(Exception ex)
            {
                Serilog.Log.Error(ex, "[LocalDeletionDetector] Failed to delete remote item {RemoteId}: {Error}", remoteId, ex.Message);
            }
        }
    }
}
