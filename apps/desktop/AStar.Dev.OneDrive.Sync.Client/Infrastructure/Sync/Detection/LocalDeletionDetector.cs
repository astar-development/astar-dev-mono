using System.Collections.Concurrent;
using System.IO.Abstractions;
using System.Reactive;
using AStar.Dev.FunctionalParadigm;
using AStar.Dev.Infrastructure.AppDb.Entities;
using AStar.Dev.OneDrive.Sync.Client.Data.Repositories;
using AStar.Dev.OneDrive.Sync.Client.Infrastructure.Graph;
using AStar.Dev.OneDrive.Sync.Client.Infrastructure.Logging;
using Microsoft.Extensions.Logging;
using AccountId = AStar.Dev.Infrastructure.AppDb.Entities.AccountId;

namespace AStar.Dev.OneDrive.Sync.Client.Infrastructure.Sync.Detection;

/// <inheritdoc />
public sealed class LocalDeletionDetector(IGraphService graphService, ISyncedItemRepository syncedItemRepository, IFileSystem fileSystem, ILogger<LocalDeletionDetector> logger) : ILocalDeletionDetector
{
    /// <inheritdoc />
    public async Task DetectAndApplyAsync(AccountId accountId, Func<CancellationToken, Task<string>> tokenFactory, ConcurrentDictionary<string, SyncedItemEntity> syncedItems, CancellationToken cancellationToken)
    {
        List<OneDriveItemId> successfullyDeletedIds = [];

        foreach (var (remoteId, knownItem) in syncedItems)
        {
            if (knownItem.IsFolder) continue;
            if (cancellationToken.IsCancellationRequested) break;
            if (fileSystem.File.Exists(knownItem.LocalPath)) continue;

            OneDriveSyncClientMessages.LocalDeletionDetectorDeleted(logger, knownItem.RemotePath);

            try
            {
                var deleteResult = await graphService.DeleteItemAsync(accountId.Value, tokenFactory, remoteId, cancellationToken).ConfigureAwait(false);

                deleteResult.Match(
                    _ =>
                    {
                        OneDriveSyncClientMessages.LocalDeletionDetectorRemoteDeleted(logger, remoteId);
                        successfullyDeletedIds.Add(knownItem.RemoteItemId);
                        return Unit.Default;
                    },
                    deleteError =>
                    {
                        OneDriveSyncClientMessages.LocalDeletionDetectorDeleteFailed(logger, remoteId, deleteError);
                        return Unit.Default;
                    });
            }
            catch (Exception ex)
            {
                OneDriveSyncClientMessages.LocalDeletionDetectorDeleteFailed(logger, remoteId, ex.Message, ex);
            }
        }

        if (successfullyDeletedIds.Count > 0)
            await syncedItemRepository.DeleteManyByRemoteIdAsync(accountId, successfullyDeletedIds, cancellationToken).ConfigureAwait(false);
    }
}
