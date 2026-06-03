using System.IO.Abstractions;
using AStar.Dev.Functional.Extensions;
using AStar.Dev.OneDrive.Sync.Client.Accounts;
using AStar.Dev.OneDrive.Sync.Client.Conflicts;
using AStar.Dev.OneDrive.Sync.Client.Data.Entities;
using AStar.Dev.OneDrive.Sync.Client.Domain;
using AStar.Dev.OneDrive.Sync.Client.Infrastructure.Logging;
using AStar.Dev.Utilities;
using Microsoft.Extensions.Logging;
using AccountId = AStar.Dev.OneDrive.Sync.Client.Data.Entities.AccountId;

namespace AStar.Dev.OneDrive.Sync.Client.Infrastructure.Sync.Jobs;

/// <inheritdoc />
public sealed class DownloadJobBuilder(ISyncedItemRegistrar syncedItemRegistrar, IFileSystem fileSystem, ILogger<DownloadJobBuilder> logger) : IDownloadJobBuilder
{
    /// <inheritdoc />
    public async Task<IReadOnlyList<SyncJob>> BuildAsync(OneDriveAccount account, IReadOnlyList<DeltaItem> items, IReadOnlyList<SyncRuleEntity> rules, Dictionary<string, SyncedItemEntity> syncedItems, Func<SyncConflict, Task> onConflict, CancellationToken ct)
    {
        List<SyncJob> jobs = [];

        foreach (var item in items.TakeWhile(_ => !ct.IsCancellationRequested))
        {
            if (!SyncRuleEvaluator.IsIncluded(item.Path.EffectivePath, rules))
                continue;

            if (item is FolderDeltaItem folderItem)
            {
                string folderLocalPath = BuildLocalPath(account.SyncConfig.Match(v => v, () => throw new InvalidOperationException("SyncConfig is None")).LocalSyncPath.Value, folderItem.Path.EffectivePath.TrimStart('/'));
                await syncedItemRegistrar.RegisterFolderAsync(account.Id, folderItem, folderItem.Path.EffectivePath, folderLocalPath, syncedItems, ct).ConfigureAwait(false);
                continue;
            }

            if (item is not FileDeltaItem fileItem)
                continue;

            var job = await ProcessFileItemAsync(account, fileItem, syncedItems, onConflict, ct).ConfigureAwait(false);
            if (job is not null)
                jobs.Add(job);
        }

        return jobs;
    }

    private async Task<SyncJob?> ProcessFileItemAsync(OneDriveAccount account, FileDeltaItem item, Dictionary<string, SyncedItemEntity> syncedItems, Func<SyncConflict, Task> onConflict, CancellationToken ct)
    {
        string localPath = BuildLocalPath(account.SyncConfig.Match(v => v, () => throw new InvalidOperationException("SyncConfig is None")).LocalSyncPath.Value, item.Path.EffectivePath.TrimStart('/'));
        syncedItems.TryGetValue(item.Id.Id, out var knownItem);

        if (knownItem?.Tags.ETag is Option<string>.Some && knownItem.Tags.ETag == item.VersionInfo.ETag && fileSystem.File.Exists(localPath))
        {
            OneDriveSyncClientMessages.DownloadETagMatch(logger, item.Path.EffectivePath);
            return null;
        }

        if (knownItem is not null && fileSystem.File.Exists(localPath))
        {
            var localModified = new DateTimeOffset(fileSystem.FileInfo.New(localPath).LastWriteTimeUtc, TimeSpan.Zero);
            bool isConflict = localModified > knownItem.RemoteModifiedAt.AddSeconds(5);

            if (isConflict)
                return await BuildConflictJobAsync(account, item, localPath, localModified, onConflict).ConfigureAwait(false);

            bool remoteUnchanged = item.LastModified.Match(lm => lm <= knownItem.RemoteModifiedAt.AddSeconds(5), () => true);
            if (remoteUnchanged)
                return null;
        }
        else if (knownItem is null && fileSystem.File.Exists(localPath))
        {
            await syncedItemRegistrar.RegisterPhantomAsync(account.Id, item, item.Path.EffectivePath, localPath, syncedItems, ct).ConfigureAwait(false);
            return null;
        }

        return BuildDownloadJob(account.Id, item, localPath);
    }

    private async Task<SyncJob?> BuildConflictJobAsync(OneDriveAccount account, FileDeltaItem item, string localPath, DateTimeOffset localModified, Func<SyncConflict, Task> onConflict)
    {
        var conflict = BuildConflict(account, item, localPath, localModified);
        await onConflict(conflict).ConfigureAwait(false);

        var outcome = ConflictResolver.Resolve(account.SyncConfig.Match(v => v, () => throw new InvalidOperationException("SyncConfig is None")).ConflictPolicy, localModified, item.LastModified.MapOrDefault(v => v, DateTimeOffset.MinValue));

        return outcome switch
        {
            ConflictOutcome.UseRemote => BuildDownloadJob(account.Id, item, localPath),
            ConflictOutcome.UseLocal => BuildUploadJob(account.Id, item, localPath, localModified),
            ConflictOutcome.KeepBoth => BuildKeepBothJob(account.Id, item, localPath, localModified),
            _ => null
        };
    }

    private static DownloadSyncJob BuildDownloadJob(AccountId accountId, FileDeltaItem item, string localPath)
    {
        var remote = RemoteItemRefFactory.Create(accountId, new OneDriveFolderId(string.Empty), item.Id);
        var target = SyncFileTargetFactory.Create(localPath, item.Path.EffectivePath);
        var metadata = SyncFileMetadataFactory.Create(item.Size, item.LastModified.MapOrDefault(v => v, DateTimeOffset.MinValue), Option.Some(item.VersionInfo));

        return SyncJobFactory.CreateDownload(remote, target, metadata, item.DownloadUrl);
    }

    private UploadSyncJob BuildUploadJob(AccountId accountId, FileDeltaItem item, string localPath, DateTimeOffset localModified)
    {
        var remote = RemoteItemRefFactory.Create(accountId, new OneDriveFolderId(string.Empty), item.Id);
        var target = SyncFileTargetFactory.Create(localPath, item.Path.EffectivePath);
        var metadata = SyncFileMetadataFactory.Create(fileSystem.FileInfo.New(localPath).Length, localModified);

        return SyncJobFactory.CreateUpload(remote, target, metadata);
    }

    private DownloadSyncJob BuildKeepBothJob(AccountId accountId, FileDeltaItem item, string localPath, DateTimeOffset localModified)
    {
        string newName = ConflictResolver.MakeKeepBothName(localPath, localModified, fileSystem);
        fileSystem.File.Move(localPath, newName);

        return BuildDownloadJob(accountId, item, localPath);
    }

    private SyncConflict BuildConflict(OneDriveAccount account, FileDeltaItem item, string localPath, DateTimeOffset localModified)
        => new()
        {
            Remote = RemoteItemRefFactory.Create(account.Id, new OneDriveFolderId(string.Empty), item.Id),
            Target = SyncFileTargetFactory.Create(localPath, item.Path.EffectivePath),
            Snapshot = ConflictSnapshotFactory.Create(localModified, fileSystem.FileInfo.New(localPath).Length, item.LastModified.MapOrDefault(v => v, DateTimeOffset.MinValue), item.Size)
        };

    private static string BuildLocalPath(string localBasePath, string relativePath)
        => localBasePath.CombinePath(relativePath.Replace('/', Path.DirectorySeparatorChar));
}
