using System.Collections.Concurrent;
using System.IO.Abstractions;
using AStar.Dev.Infrastructure.AppDb;
using AStar.Dev.Infrastructure.AppDb.Entities;
using AStar.Dev.OneDrive.Sync.Client.Data.Repositories;
using AStar.Dev.Infrastructure.AppDb.Domain;
using AStar.Dev.OneDrive.Sync.Client.Infrastructure.Logging;
using Microsoft.Extensions.Logging;
using AccountId = AStar.Dev.Infrastructure.AppDb.Entities.AccountId;

namespace AStar.Dev.OneDrive.Sync.Client.Infrastructure.Sync.Jobs;

/// <inheritdoc />
public sealed class SyncedItemRegistrar(ISyncedItemRepository syncedItemRepository, IFileSystem fileSystem, ILogger<SyncedItemRegistrar> logger, IFileAutoCategorisor fileAutoCategorisor, ICategoryResolutionService categoryResolutionService, IFileDetailResolver fileDetailResolver, IFileClassificationRepository fileClassificationRepository) : ISyncedItemRegistrar
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
        await RegisterFileAsync(phantomItem, localPath, remotePath, mappings, ct).ConfigureAwait(false);
        syncedItems[item.Id.Id] = phantomItem;
    }

    /// <inheritdoc />
    public async Task RegisterDownloadAsync(AccountId accountId, SyncJob job, string remotePath, IReadOnlyList<FileClassificationCategory> mappings, ConcurrentDictionary<string, SyncedItemEntity> syncedItems, CancellationToken ct)
    {
        var entity = SyncedItemEntityFactory.CreateFromDownloadJob(accountId, job, remotePath);
        await RegisterFileAsync(entity, job.Target.LocalPath, remotePath, mappings, ct).ConfigureAwait(false);
        syncedItems[job.Remote.RemoteItemId.Id] = entity;
    }

    /// <inheritdoc />
    public async Task RegisterUploadAsync(AccountId accountId, UploadSyncJob job, string uploadedRemoteItemId, string remotePath, IReadOnlyList<FileClassificationCategory> mappings, ConcurrentDictionary<string, SyncedItemEntity> syncedItems, CancellationToken ct)
    {
        var entity = SyncedItemEntityFactory.CreateFromUploadJob(accountId, job, uploadedRemoteItemId, remotePath, fileSystem);
        await RegisterFileAsync(entity, job.Target.LocalPath, remotePath, mappings, ct).ConfigureAwait(false);
        syncedItems[uploadedRemoteItemId] = entity;
    }

    private async Task RegisterFileAsync(SyncedItemEntity entity, string localPath, string remotePath, IReadOnlyList<FileClassificationCategory> mappings, CancellationToken ct)
    {
        var fileDetail = await fileDetailResolver.FindOrCreateAsync(localPath, entity.SizeInBytes, ct).ConfigureAwait(false);
        entity.FileDetailId = fileDetail.Id;
        _ = await syncedItemRepository.UpsertAsync(entity, ct).ConfigureAwait(false);

        if (await fileClassificationRepository.HasClassificationsAsync(fileDetail.Id, ct).ConfigureAwait(false))
            return;

        var categoryIds = await ResolveClassificationsAsync(remotePath, mappings, ct).ConfigureAwait(false);
        await fileClassificationRepository.AddClassificationsAsync(fileDetail.Id, categoryIds, ct).ConfigureAwait(false);
    }

    private async Task<IReadOnlyList<int>> ResolveClassificationsAsync(string remotePath, IReadOnlyList<FileClassificationCategory> mappings, CancellationToken ct)
    {
        var analyserResult = fileAutoCategorisor.Categorise(remotePath);
        var classifications = ClassificationCombiner.Combine(FileClassifier.Classify(remotePath, mappings), analyserResult.Match(c => (IReadOnlyList<FileClassification>)[c], () => []));

        return await categoryResolutionService.ResolveManyAsync(classifications, ct).ConfigureAwait(false);
    }
}
