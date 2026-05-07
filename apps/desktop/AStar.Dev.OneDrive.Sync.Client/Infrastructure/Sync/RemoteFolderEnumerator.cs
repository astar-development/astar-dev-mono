using System.IO.Abstractions;
using AStar.Dev.Functional.Extensions;
using AStar.Dev.OneDrive.Sync.Client.Conflicts;
using AStar.Dev.OneDrive.Sync.Client.Home;
using AStar.Dev.OneDrive.Sync.Client.Data.Entities;
using AStar.Dev.OneDrive.Sync.Client.Data.Repositories;
using AStar.Dev.OneDrive.Sync.Client.Domain;
using AStar.Dev.OneDrive.Sync.Client.Infrastructure.Graph;
using AStar.Dev.OneDrive.Sync.Client.Accounts;
using AStar.Dev.Utilities;
using AccountId = AStar.Dev.OneDrive.Sync.Client.Data.Entities.AccountId;

namespace AStar.Dev.OneDrive.Sync.Client.Infrastructure.Sync;

/// <inheritdoc />
public sealed class RemoteFolderEnumerator(IGraphService graphService, ISyncRuleRepository syncRuleRepository, ISyncedItemRepository syncedItemRepository, IFileSystem fileSystem) : IRemoteFolderEnumerator
{
    /// <inheritdoc />
    public async Task<RemoteEnumerationResult> EnumerateAsync(OneDriveAccount account, string accessToken, Func<SyncConflict, Task> onConflict, CancellationToken ct)
    {
        var rules = await syncRuleRepository.GetByAccountIdAsync(account.Id, ct).ConfigureAwait(false);

        if(rules.Count == 0)
        {
            Serilog.Log.Information("[RemoteFolderEnumerator] No sync rules configured for {Email} — nothing to sync", account.Profile.Email);

            return new RemoteEnumerationResult([], new HashSet<string>(StringComparer.OrdinalIgnoreCase), [], [], HadNoRules: true);
        }

        var syncedItems = await syncedItemRepository.GetAllByAccountAsync(account.Id, ct).ConfigureAwait(false);
        var driveIdResult = await graphService.GetDriveIdAsync(accessToken, ct).ConfigureAwait(false);
        var driveId = driveIdResult.Match(
            id    => id,
            error => throw new InvalidOperationException($"Failed to retrieve drive ID: {error}")
        );

        var includeRules     = rules.Where(r => r.RuleType == RuleType.Include).ToList();
        var rootIncludeRules = includeRules
            .Where(rule => !includeRules.Any(other => other.RemotePath != rule.RemotePath && rule.RemotePath.StartsWith(other.RemotePath + "/", StringComparison.OrdinalIgnoreCase)))
            .ToList();

        List<SyncJob> downloadJobs  = [];
        var seenRemoteIds           = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach(var rule in rootIncludeRules)
        {
            if(ct.IsCancellationRequested)
                break;

            var folderId = await ResolveAndBackFillFolderIdAsync(account.Id, rule, syncedItems, accessToken, driveId, ct).ConfigureAwait(false);

            if(folderId is null)
            {
                Serilog.Log.Warning("[RemoteFolderEnumerator] Cannot resolve folder ID for rule path {Path} — skipping", rule.RemotePath);
                continue;
            }

            Serilog.Log.Information("[RemoteFolderEnumerator] Enumerating {Path} for {Email}", rule.RemotePath, account.Profile.Email);
            var items = await graphService.EnumerateFolderAsync(accessToken, driveId, folderId, rule.RemotePath, ct).ConfigureAwait(false);
            Serilog.Log.Information("[RemoteFolderEnumerator] Enumerated {Count} items under {Path}", items.Count, rule.RemotePath);

            await ProcessItemsForRuleAsync(account, items, rules, syncedItems, downloadJobs, onConflict, seenRemoteIds, ct).ConfigureAwait(false);
        }

        return new RemoteEnumerationResult(downloadJobs, seenRemoteIds, syncedItems, rules);
    }

    private async Task<string?> ResolveAndBackFillFolderIdAsync(AccountId accountId, SyncRuleEntity rule, Dictionary<string, SyncedItemEntity> syncedItems, string accessToken, DriveId driveId, CancellationToken ct)
    {
        var folderId = rule.RemoteItemId
            ?? TryResolveFromSyncedItems(syncedItems, rule.RemotePath)
            ?? await graphService.GetFolderIdByPathAsync(accessToken, driveId, rule.RemotePath, ct).ConfigureAwait(false);

        if(folderId is not null && folderId != rule.RemoteItemId)
        {
            Serilog.Log.Debug("[RemoteFolderEnumerator] Back-filling RemoteItemId for rule {Path}", rule.RemotePath);
            await syncRuleRepository.UpsertAsync(accountId, rule.RemotePath, RuleType.Include, folderId, ct).ConfigureAwait(false);
        }

        return folderId;
    }

    private async Task ProcessItemsForRuleAsync(OneDriveAccount account, IReadOnlyList<DeltaItem> items, IReadOnlyList<SyncRuleEntity> rules, Dictionary<string, SyncedItemEntity> syncedItems, List<SyncJob> downloadJobs, Func<SyncConflict, Task> onConflict, HashSet<string> seenRemoteIds, CancellationToken ct)
    {
        foreach(var item in items.TakeWhile(_ => !ct.IsCancellationRequested))
        {
            seenRemoteIds.Add(item.Id.Id);

            if(!SyncRuleEvaluator.IsIncluded(item.Path.EffectivePath, rules))
                continue;

            if(item.IsFolder)
            {
                await HandleFolderAsync(account.Id, item, item.Path.EffectivePath, account.SyncConfig!.LocalSyncPath.Value, syncedItems, ct).ConfigureAwait(false);
                continue;
            }

            await ProcessFileItemAsync(account, item, rules, syncedItems, downloadJobs, onConflict, ct).ConfigureAwait(false);
        }
    }

    private async Task ProcessFileItemAsync(OneDriveAccount account, DeltaItem item, IReadOnlyList<SyncRuleEntity> rules, Dictionary<string, SyncedItemEntity> syncedItems, List<SyncJob> downloadJobs, Func<SyncConflict, Task> onConflict, CancellationToken ct)
    {
        string localPath = BuildLocalPath(account.SyncConfig!.LocalSyncPath.Value, item.Path.EffectivePath.TrimStart('/'));

        syncedItems.TryGetValue(item.Id.Id, out var knownItem);

        if(knownItem?.Tags.ETag is not null && knownItem.Tags.ETag == item.VersionInfo.ETag && fileSystem.File.Exists(localPath))
        {
            Serilog.Log.Debug("[RemoteFolderEnumerator] ETag match — skipping unchanged file {Path}", item.Path.RelativePath);
            return;
        }

        if(knownItem is not null && fileSystem.File.Exists(localPath))
        {
            var localModified = new DateTimeOffset(fileSystem.FileInfo.New(localPath).LastWriteTimeUtc, TimeSpan.Zero);
            bool isConflict   = localModified > knownItem.RemoteModifiedAt.AddSeconds(5);

            if(isConflict)
            {
                var conflict = BuildConflict(account, item, localPath, localModified);
                await HandleConflictAsync(account, item, localPath, localModified, conflict, downloadJobs, onConflict).ConfigureAwait(false);
                return;
            }
        }
        else if(knownItem is null && fileSystem.File.Exists(localPath))
        {
            Serilog.Log.Debug("[RemoteFolderEnumerator] File exists locally without SyncedItemEntity — treating as synced: {Path}", localPath);
            var phantomItem = SyncedItemEntityFactory.Create(account.Id, item, item.Path.EffectivePath, localPath);
            await syncedItemRepository.UpsertAsync(phantomItem, ct).ConfigureAwait(false);
            syncedItems[item.Id.Id] = phantomItem;
            return;
        }

        var remote = RemoteItemRefFactory.Create(account.Id, new OneDriveFolderId(string.Empty), item.Id);
        var target = SyncFileTargetFactory.Create(localPath, item.Path.EffectivePath);
        var metadata = SyncFileMetadataFactory.Create(item.Size, item.LastModified ?? DateTimeOffset.MinValue);

        downloadJobs.Add(SyncJobFactory.CreateDownload(remote, target, metadata, item.DownloadUrl));
    }

    private async Task HandleFolderAsync(AccountId accountId, DeltaItem item, string remotePath, string localBasePath, Dictionary<string, SyncedItemEntity> syncedItems, CancellationToken ct)
    {
        string localPath = BuildLocalPath(localBasePath, remotePath.TrimStart('/'));
        _ = fileSystem.Directory.CreateDirectory(localPath);

        var entity = SyncedItemEntityFactory.Create(accountId, item, remotePath, localPath);
        await syncedItemRepository.UpsertAsync(entity, ct).ConfigureAwait(false);
        syncedItems[item.Id.Id] = entity;
    }

    private async Task HandleConflictAsync(OneDriveAccount account, DeltaItem item, string localPath, DateTimeOffset localModified, SyncConflict conflict, List<SyncJob> downloadJobs, Func<SyncConflict, Task> onConflict)
    {
        await onConflict(conflict).ConfigureAwait(false);

        var outcome = ConflictResolver.Resolve(account.SyncConfig!.ConflictPolicy, localModified, item.LastModified ?? DateTimeOffset.MinValue);

        switch(outcome)
        {
            case ConflictOutcome.Skip:
                break;

            case ConflictOutcome.UseRemote:
                var remoteR = RemoteItemRefFactory.Create(account.Id, new OneDriveFolderId(string.Empty), item.Id);
                var targetR = SyncFileTargetFactory.Create(localPath, item.Path.EffectivePath);
                var metadataR = SyncFileMetadataFactory.Create(item.Size, item.LastModified ?? DateTimeOffset.MinValue);

                downloadJobs.Add(SyncJobFactory.CreateDownload(remoteR, targetR, metadataR, item.DownloadUrl));
                break;

            case ConflictOutcome.UseLocal:
                var remoteL = RemoteItemRefFactory.Create(account.Id, new OneDriveFolderId(string.Empty), item.Id);
                var targetL = SyncFileTargetFactory.Create(localPath, item.Path.EffectivePath);
                var metadataL = SyncFileMetadataFactory.Create(fileSystem.FileInfo.New(localPath).Length, localModified);

                downloadJobs.Add(SyncJobFactory.CreateUpload(remoteL, targetL, metadataL));
                break;

            case ConflictOutcome.KeepBoth:
                string newName = ConflictResolver.MakeKeepBothName(localPath, localModified, fileSystem);
                fileSystem.File.Move(localPath, newName);

                var remoteK = RemoteItemRefFactory.Create(account.Id, new OneDriveFolderId(string.Empty), item.Id);
                var targetK = SyncFileTargetFactory.Create(localPath, item.Path.EffectivePath);
                var metadataK = SyncFileMetadataFactory.Create(item.Size, item.LastModified ?? DateTimeOffset.MinValue);

                downloadJobs.Add(SyncJobFactory.CreateDownload(remoteK, targetK, metadataK, item.DownloadUrl));
                break;
        }
    }

    private static string? TryResolveFromSyncedItems(Dictionary<string, SyncedItemEntity> syncedItems, string remotePath)
        => syncedItems.Values.FirstOrDefault(i => i.IsFolder && string.Equals(i.RemotePath, remotePath, StringComparison.OrdinalIgnoreCase))?.RemoteItemId.Id;

    private SyncConflict BuildConflict(OneDriveAccount account, DeltaItem item, string localPath, DateTimeOffset localModified)
        => new()
        {
            Remote   = RemoteItemRefFactory.Create(account.Id, new OneDriveFolderId(string.Empty), item.Id),
            Target   = SyncFileTargetFactory.Create(localPath, item.Path.EffectivePath),
            Snapshot = ConflictSnapshotFactory.Create(localModified, fileSystem.FileInfo.New(localPath).Length, item.LastModified ?? DateTimeOffset.MinValue, item.Size)
        };

    private static string BuildLocalPath(string localBasePath, string relativePath)
        => localBasePath.CombinePath(relativePath.Replace('/', Path.DirectorySeparatorChar));
}
