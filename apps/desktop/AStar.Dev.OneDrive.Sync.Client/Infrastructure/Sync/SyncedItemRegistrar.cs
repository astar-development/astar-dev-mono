using System.IO.Abstractions;
using AStar.Dev.OneDrive.Sync.Client.Data.Entities;
using AStar.Dev.OneDrive.Sync.Client.Data.Repositories;
using AStar.Dev.OneDrive.Sync.Client.Domain;
using AStar.Dev.OneDrive.Sync.Client.Infrastructure.Logging;
using Microsoft.Extensions.Logging;
using AccountId = AStar.Dev.OneDrive.Sync.Client.Data.Entities.AccountId;

namespace AStar.Dev.OneDrive.Sync.Client.Infrastructure.Sync;

/// <inheritdoc />
public sealed class SyncedItemRegistrar(ISyncedItemRepository syncedItemRepository, IFileSystem fileSystem, ILogger<SyncedItemRegistrar> logger) : ISyncedItemRegistrar
{
    /// <inheritdoc />
    public async Task RegisterFolderAsync(AccountId accountId, FolderDeltaItem item, string remotePath, string localPath, Dictionary<string, SyncedItemEntity> syncedItems, CancellationToken ct)
    {
        _ = fileSystem.Directory.CreateDirectory(localPath);
        var entity = SyncedItemEntityFactory.Create(accountId, item, remotePath, localPath);
        await syncedItemRepository.UpsertAsync(entity, ct).ConfigureAwait(false);
        syncedItems[item.Id.Id] = entity;
    }

    /// <inheritdoc />
    public async Task RegisterPhantomAsync(AccountId accountId, FileDeltaItem item, string remotePath, string localPath, Dictionary<string, SyncedItemEntity> syncedItems, CancellationToken ct)
    {
        OneDriveSyncClientMessages.SyncedItemLocalExists(logger, localPath);
        var phantomItem = SyncedItemEntityFactory.Create(accountId, item, remotePath, localPath);
        await syncedItemRepository.UpsertAsync(phantomItem, ct).ConfigureAwait(false);
        syncedItems[item.Id.Id] = phantomItem;
    }
}
