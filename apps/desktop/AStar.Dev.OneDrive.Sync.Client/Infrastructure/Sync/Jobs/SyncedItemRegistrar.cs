using System.Collections.Concurrent;
using System.IO.Abstractions;
using AStar.Dev.OneDrive.Sync.Client.Data.Entities;
using AStar.Dev.OneDrive.Sync.Client.Data.Repositories;
using AStar.Dev.OneDrive.Sync.Client.Domain;
using AStar.Dev.OneDrive.Sync.Client.Infrastructure.Logging;
using Microsoft.Extensions.Logging;
using AccountId = AStar.Dev.OneDrive.Sync.Client.Data.Entities.AccountId;

namespace AStar.Dev.OneDrive.Sync.Client.Infrastructure.Sync.Jobs;

/// <inheritdoc />
public sealed class SyncedItemRegistrar(ISyncedItemRepository syncedItemRepository, IFileSystem fileSystem, ILogger<SyncedItemRegistrar> logger, IFileAutoCategorisor fileAutoCategorisor, ICategoryResolutionService categoryResolutionService) : ISyncedItemRegistrar
{
    /// <inheritdoc />
    public async Task RegisterFolderAsync(AccountId accountId, FolderDeltaItem item, string remotePath, string localPath, ConcurrentDictionary<string, SyncedItemEntity> syncedItems, CancellationToken ct)
    {
        _ = fileSystem.Directory.CreateDirectory(localPath);
        var entity = SyncedItemEntityFactory.Create(accountId, item, remotePath, localPath);
        _ = await syncedItemRepository.UpsertAsync(entity, ct).ConfigureAwait(false);
        syncedItems[item.Id.Id] = entity;
    }

    /// <inheritdoc />
    public async Task RegisterPhantomAsync(AccountId accountId, FileDeltaItem item, string remotePath, string localPath, ConcurrentDictionary<string, SyncedItemEntity> syncedItems, IReadOnlyList<FileClassificationCategory> mappings, CancellationToken ct)
    {
        OneDriveSyncClientMessages.SyncedItemLocalExists(logger, localPath);
        var phantomItem = SyncedItemEntityFactory.Create(accountId, item, remotePath, localPath);
        int syncedItemId = await syncedItemRepository.UpsertAsync(phantomItem, ct).ConfigureAwait(false);
        var categoryIds = await ResolveClassificationsAsync(remotePath, mappings, ct).ConfigureAwait(false);
        await syncedItemRepository.UpsertFileClassificationsAsync(syncedItemId, categoryIds, ct).ConfigureAwait(false);
        syncedItems[item.Id.Id] = phantomItem;
    }

    /// <inheritdoc />
    public async Task RegisterDownloadAsync(AccountId accountId, SyncJob job, string remotePath, IReadOnlyList<FileClassificationCategory> mappings, ConcurrentDictionary<string, SyncedItemEntity> syncedItems, CancellationToken ct)
    {
        var entity = SyncedItemEntityFactory.CreateFromDownloadJob(accountId, job, remotePath);
        var categoryIds = await ResolveClassificationsAsync(remotePath, mappings, ct).ConfigureAwait(false);
        await syncedItemRepository.UpsertWithClassificationsAsync(entity, categoryIds, ct).ConfigureAwait(false);
        syncedItems[job.Remote.RemoteItemId.Id] = entity;
    }

    /// <inheritdoc />
    public async Task RegisterUploadAsync(AccountId accountId, UploadSyncJob job, string uploadedRemoteItemId, string remotePath, IReadOnlyList<FileClassificationCategory> mappings, ConcurrentDictionary<string, SyncedItemEntity> syncedItems, CancellationToken ct)
    {
        var entity = SyncedItemEntityFactory.CreateFromUploadJob(accountId, job, uploadedRemoteItemId, remotePath, fileSystem);
        var categoryIds = await ResolveClassificationsAsync(remotePath, mappings, ct).ConfigureAwait(false);
        await syncedItemRepository.UpsertWithClassificationsAsync(entity, categoryIds, ct).ConfigureAwait(false);
        syncedItems[uploadedRemoteItemId] = entity;
    }

    private async Task<IReadOnlyList<int>> ResolveClassificationsAsync(string remotePath, IReadOnlyList<FileClassificationCategory> mappings, CancellationToken ct)
    {
        var analyserResult = fileAutoCategorisor.Categorise(remotePath);
        var classifications = ClassificationCombiner.Combine(FileClassifier.Classify(remotePath, mappings), analyserResult.Match(c => (IReadOnlyList<FileClassification>)[c], () => []));

        return await categoryResolutionService.ResolveManyAsync(classifications, ct).ConfigureAwait(false);
    }
}
