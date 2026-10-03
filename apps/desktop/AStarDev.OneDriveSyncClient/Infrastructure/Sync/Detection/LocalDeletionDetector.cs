using System.Collections.Concurrent;
using System.IO.Abstractions;
using System.Reactive;
using Unit = System.Reactive.Unit;
using AStarDev.FunctionalParadigm;
using AStar.Dev.Infrastructure.AppDb.Entities;
using AStarDev.OneDriveSyncClient.Data.Repositories;
using AStarDev.OneDriveSyncClient.Infrastructure.Graph;
using AStarDev.OneDriveSyncClient.Infrastructure.Logging;
using Microsoft.Extensions.Logging;
using AccountId = AStar.Dev.Infrastructure.AppDb.Entities.AccountId;

namespace AStarDev.OneDriveSyncClient.Infrastructure.Sync.Detection;

/// <inheritdoc />
public sealed class LocalDeletionDetector(IGraphService graphService, ISyncedItemRepository syncedItemRepository, IFileSystem fileSystem, ILogger<LocalDeletionDetector> logger) : ILocalDeletionDetector
{
    /// <inheritdoc />
    public async Task DetectAndApplyAsync(AccountId accountId, Func<CancellationToken, Task<string>> tokenFactory, ConcurrentDictionary<string, SyncedItemEntity> syncedItems, CancellationToken cancellationToken)
    {
        var missingItems = syncedItems.Values.Where(HasMissingLocalPath).ToList();
        List<OneDriveItemId> successfullyDeletedIds = [];
        HashSet<string> coveredRemoteIds = new(StringComparer.OrdinalIgnoreCase);

        foreach (var knownItem in missingItems.OrderBy(item => item.IsFolder ? 0 : 1).ThenBy(item => item.RemotePath.Length))
        {
            if (cancellationToken.IsCancellationRequested) break;
            if (coveredRemoteIds.Contains(knownItem.RemoteItemId.Value)) continue;

            if (knownItem.IsFolder)
                OneDriveSyncClientMessages.LocalDeletionDetectorFolderDeleted(logger, knownItem.RemotePath);
            else
                OneDriveSyncClientMessages.LocalDeletionDetectorDeleted(logger, knownItem.RemotePath);

            try
            {
                var deleteResult = await graphService.DeleteItemAsync(accountId.Value, tokenFactory, knownItem.RemoteItemId.Value, cancellationToken).ConfigureAwait(false);

                deleteResult.Match(
                    _ =>
                    {
                        OneDriveSyncClientMessages.LocalDeletionDetectorRemoteDeleted(logger, knownItem.RemoteItemId.Value);
                        var coveredItems = knownItem.IsFolder
                            ? missingItems.Where(item => IsSameOrDescendantPath(item.RemotePath, knownItem.RemotePath))
                            : [knownItem];

                        foreach (var coveredItem in coveredItems)
                        {
                            coveredRemoteIds.Add(coveredItem.RemoteItemId.Value);
                            successfullyDeletedIds.Add(coveredItem.RemoteItemId);
                        }

                        return Unit.Default;
                    },
                    deleteError =>
                    {
                        OneDriveSyncClientMessages.LocalDeletionDetectorDeleteFailed(logger, knownItem.RemoteItemId.Value, deleteError);
                        return Unit.Default;
                    });
            }
            catch (Exception ex)
            {
                OneDriveSyncClientMessages.LocalDeletionDetectorDeleteFailed(logger, knownItem.RemoteItemId.Value, ex.Message, ex);
            }
        }

        if (successfullyDeletedIds.Count > 0)
        {
            await syncedItemRepository.DeleteManyByRemoteIdAsync(accountId, successfullyDeletedIds, cancellationToken).ConfigureAwait(false);

            foreach (var remoteId in successfullyDeletedIds)
                syncedItems.TryRemove(remoteId.Value, out _);
        }
    }

    private bool HasMissingLocalPath(SyncedItemEntity item)
        => item.IsFolder ? !fileSystem.Directory.Exists(item.LocalPath) : !fileSystem.File.Exists(item.LocalPath);

    private static bool IsSameOrDescendantPath(string path, string parentPath)
        => string.Equals(path, parentPath, StringComparison.OrdinalIgnoreCase)
            || path.StartsWith(parentPath.TrimEnd('/') + "/", StringComparison.OrdinalIgnoreCase);
}
