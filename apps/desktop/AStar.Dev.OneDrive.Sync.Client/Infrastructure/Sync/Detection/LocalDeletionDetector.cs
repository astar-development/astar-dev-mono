using System.IO.Abstractions;
using System.Reactive;
using AStar.Dev.OneDrive.Sync.Client.Data.Entities;
using AStar.Dev.OneDrive.Sync.Client.Data.Repositories;
using AStar.Dev.OneDrive.Sync.Client.Infrastructure.Graph;
using AStar.Dev.OneDrive.Sync.Client.Infrastructure.Logging;
using Microsoft.Extensions.Logging;
using AccountId = AStar.Dev.OneDrive.Sync.Client.Data.Entities.AccountId;

namespace AStar.Dev.OneDrive.Sync.Client.Infrastructure.Sync.Detection;

/// <inheritdoc />
public sealed class LocalDeletionDetector(IGraphService graphService, ISyncedItemRepository syncedItemRepository, IFileSystem fileSystem, ILogger<LocalDeletionDetector> logger) : ILocalDeletionDetector
{
    /// <inheritdoc />
    public async Task DetectAndApplyAsync(AccountId accountId, Func<CancellationToken, Task<string>> tokenFactory, Dictionary<string, SyncedItemEntity> syncedItems, CancellationToken ct)
    {
        foreach (var (remoteId, knownItem) in syncedItems)
        {
            if (knownItem.IsFolder) continue;
            if (ct.IsCancellationRequested) break;
            if (fileSystem.File.Exists(knownItem.LocalPath)) continue;

            OneDriveSyncClientMessages.LocalDeletionDetectorDeleted(logger, knownItem.RemotePath);

            try
            {
                var deleteResult = await graphService.DeleteItemAsync(accountId.Id, tokenFactory, remoteId, ct).ConfigureAwait(false);

                await deleteResult.MatchAsync(
                    async _ =>
                    {
                        OneDriveSyncClientMessages.LocalDeletionDetectorRemoteDeleted(logger, remoteId);
                        await syncedItemRepository.DeleteByRemoteIdAsync(accountId, knownItem.RemoteItemId, ct).ConfigureAwait(false);
                        return Unit.Default;
                    },
                    deleteError =>
                    {
                        OneDriveSyncClientMessages.LocalDeletionDetectorDeleteFailed(logger, remoteId, deleteError);
                        return Unit.Default;
                    }).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                OneDriveSyncClientMessages.LocalDeletionDetectorDeleteFailed(logger, remoteId, ex.Message, ex);
            }
        }
    }
}
