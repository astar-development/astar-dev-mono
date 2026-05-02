using System.IO.Abstractions;
using AStar.Dev.OneDrive.Sync.Client.Conflicts;
using AStar.Dev.OneDrive.Sync.Client.Data.Entities;
using AStar.Dev.OneDrive.Sync.Client.Data.Repositories;
using AStar.Dev.OneDrive.Sync.Client.Domain;
using AStar.Dev.OneDrive.Sync.Client.Infrastructure.Graph;
using AStar.Dev.OneDrive.Sync.Client.Accounts;
using AStar.Dev.Utilities;

namespace AStar.Dev.OneDrive.Sync.Client.Infrastructure.Sync;

/// <inheritdoc />
public sealed class RemoteFolderEnumerator(IGraphService graphService, ISyncRuleRepository syncRuleRepository, ISyncedItemRepository syncedItemRepository, IFileSystem fileSystem) : IRemoteFolderEnumerator
{
    /// <inheritdoc />
    public async Task<RemoteEnumerationResult> EnumerateAsync(OneDriveAccount account, string accessToken, Func<SyncConflict, Task> onConflict, CancellationToken ct)
    {
        var rules = await syncRuleRepository.GetByAccountIdAsync(account.Id, ct);

        if(rules.Count == 0)
        {
            Serilog.Log.Information("[RemoteFolderEnumerator] No sync rules configured for {Email} — nothing to sync", account.Email);

            return new RemoteEnumerationResult([], new HashSet<string>(StringComparer.OrdinalIgnoreCase), [], [], HadNoRules: true);
        }

        var syncedItems = await syncedItemRepository.GetAllByAccountAsync(account.Id, ct);
        var driveId     = await graphService.GetDriveIdAsync(accessToken, ct);

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

            var folderId = await ResolveAndBackFillFolderIdAsync(account.Id, rule, syncedItems, accessToken, driveId, ct);

            if(folderId is null)
            {
                Serilog.Log.Warning("[RemoteFolderEnumerator] Cannot resolve folder ID for rule path {Path} — skipping", rule.RemotePath);
                continue;
            }

            Serilog.Log.Information("[RemoteFolderEnumerator] Enumerating {Path} for {Email}", rule.RemotePath, account.Email);
            var items = await graphService.EnumerateFolderAsync(accessToken, driveId, folderId, rule.RemotePath, ct);
            Serilog.Log.Information("[RemoteFolderEnumerator] Enumerated {Count} items under {Path}", items.Count, rule.RemotePath);

            await ProcessItemsForRuleAsync(account, items, rules, syncedItems, downloadJobs, onConflict, seenRemoteIds, ct);
        }

        return new RemoteEnumerationResult(downloadJobs, seenRemoteIds, syncedItems, rules);
    }

    private async Task<string?> ResolveAndBackFillFolderIdAsync(AccountId accountId, SyncRuleEntity rule, Dictionary<string, SyncedItemEntity> syncedItems, string accessToken, string driveId, CancellationToken ct)
    {
        var folderId = rule.RemoteItemId
            ?? TryResolveFromSyncedItems(syncedItems, rule.RemotePath)
            ?? await graphService.GetFolderIdByPathAsync(accessToken, driveId, rule.RemotePath, ct);

        if(folderId is not null && folderId != rule.RemoteItemId)
        {
            Serilog.Log.Debug("[RemoteFolderEnumerator] Back-filling RemoteItemId for rule {Path}", rule.RemotePath);
            await syncRuleRepository.UpsertAsync(accountId, rule.RemotePath, RuleType.Include, folderId, ct);
        }

        return folderId;
    }

    private async Task ProcessItemsForRuleAsync(OneDriveAccount account, IReadOnlyList<DeltaItem> items, IReadOnlyList<SyncRuleEntity> rules, Dictionary<string, SyncedItemEntity> syncedItems, List<SyncJob> downloadJobs, Func<SyncConflict, Task> onConflict, HashSet<string> seenRemoteIds, CancellationToken ct)
    {
        foreach(var item in items.TakeWhile(_ => !ct.IsCancellationRequested))
        {
            seenRemoteIds.Add(item.Id);

            if(!SyncRuleEvaluator.IsIncluded(item.RelativePath ?? item.Name, rules))
                continue;

            if(item.IsFolder)
            {
                await HandleFolderAsync(account.Id, item, item.RelativePath ?? item.Name, account.LocalSyncPath!.Value, syncedItems, ct);
                continue;
            }

            await ProcessFileItemAsync(account, item, rules, syncedItems, downloadJobs, onConflict, ct);
        }
    }

    private async Task ProcessFileItemAsync(OneDriveAccount account, DeltaItem item, IReadOnlyList<SyncRuleEntity> rules, Dictionary<string, SyncedItemEntity> syncedItems, List<SyncJob> downloadJobs, Func<SyncConflict, Task> onConflict, CancellationToken ct)
    {
        string localPath = BuildLocalPath(account.LocalSyncPath!.Value, (item.RelativePath ?? item.Name).TrimStart('/'));

        syncedItems.TryGetValue(item.Id, out var knownItem);

        if(knownItem?.ETag is not null && knownItem.ETag == item.ETag && fileSystem.File.Exists(localPath))
        {
            Serilog.Log.Debug("[RemoteFolderEnumerator] ETag match — skipping unchanged file {Path}", item.RelativePath);
            return;
        }

        if(knownItem is not null && fileSystem.File.Exists(localPath))
        {
            var localModified = new DateTimeOffset(fileSystem.FileInfo.New(localPath).LastWriteTimeUtc, TimeSpan.Zero);
            bool isConflict   = localModified > knownItem.RemoteModifiedAt.AddSeconds(5);

            if(isConflict)
            {
                var conflict = BuildConflict(account, item, localPath, localModified);
                await HandleConflictAsync(account, item, localPath, localModified, conflict, downloadJobs, onConflict);
                return;
            }
        }
        else if(knownItem is null && fileSystem.File.Exists(localPath))
        {
            Serilog.Log.Debug("[RemoteFolderEnumerator] File exists locally without SyncedItemEntity — treating as synced: {Path}", localPath);
            var phantomItem = SyncedItemEntityFactory.Create(account.Id, item, item.RelativePath ?? item.Name, localPath);
            await syncedItemRepository.UpsertAsync(phantomItem, ct);
            syncedItems[item.Id] = phantomItem;
            return;
        }

        downloadJobs.Add(SyncJobFactory.Create(account.Id.Id, string.Empty, item.Id, item.RelativePath ?? item.Name, localPath, SyncDirection.Download, item.Size, item.LastModified ?? DateTimeOffset.MinValue, downloadUrl: item.DownloadUrl));
    }

    private async Task HandleFolderAsync(AccountId accountId, DeltaItem item, string remotePath, string localBasePath, Dictionary<string, SyncedItemEntity> syncedItems, CancellationToken ct)
    {
        string localPath = BuildLocalPath(localBasePath, remotePath.TrimStart('/'));
        _ = fileSystem.Directory.CreateDirectory(localPath);

        var entity = SyncedItemEntityFactory.Create(accountId, item, remotePath, localPath);
        await syncedItemRepository.UpsertAsync(entity, ct);
        syncedItems[item.Id] = entity;
    }

    private async Task HandleConflictAsync(OneDriveAccount account, DeltaItem item, string localPath, DateTimeOffset localModified, SyncConflict conflict, List<SyncJob> downloadJobs, Func<SyncConflict, Task> onConflict)
    {
        await onConflict(conflict);

        var outcome = ConflictResolver.Resolve(account.ConflictPolicy, localModified, item.LastModified ?? DateTimeOffset.MinValue);

        switch(outcome)
        {
            case ConflictOutcome.Skip:
                break;

            case ConflictOutcome.UseRemote:
                downloadJobs.Add(SyncJobFactory.Create(account.Id.Id, string.Empty, item.Id, item.RelativePath ?? item.Name, localPath, SyncDirection.Download, item.Size, item.LastModified ?? DateTimeOffset.MinValue, downloadUrl: item.DownloadUrl));
                break;

            case ConflictOutcome.UseLocal:
                downloadJobs.Add(SyncJobFactory.Create(account.Id.Id, string.Empty, item.Id, item.RelativePath ?? item.Name, localPath, SyncDirection.Upload, fileSystem.FileInfo.New(localPath).Length, localModified));
                break;

            case ConflictOutcome.KeepBoth:
                string newName = ConflictResolver.MakeKeepBothName(localPath, localModified, fileSystem);
                fileSystem.File.Move(localPath, newName);
                downloadJobs.Add(SyncJobFactory.Create(account.Id.Id, string.Empty, item.Id, item.RelativePath ?? item.Name, localPath, SyncDirection.Download, item.Size, item.LastModified ?? DateTimeOffset.MinValue, downloadUrl: item.DownloadUrl));
                break;
        }
    }

    private static string? TryResolveFromSyncedItems(Dictionary<string, SyncedItemEntity> syncedItems, string remotePath)
        => syncedItems.Values.FirstOrDefault(i => i.IsFolder && string.Equals(i.RemotePath, remotePath, StringComparison.OrdinalIgnoreCase))?.RemoteItemId.Id;

    private SyncConflict BuildConflict(OneDriveAccount account, DeltaItem item, string localPath, DateTimeOffset localModified)
        => new()
        {
            AccountId      = account.Id.Id,
            FolderId       = string.Empty,
            RemoteItemId   = item.Id,
            RelativePath   = item.RelativePath ?? item.Name,
            LocalPath      = localPath,
            LocalModified  = localModified,
            RemoteModified = item.LastModified ?? DateTimeOffset.MinValue,
            LocalSize      = fileSystem.FileInfo.New(localPath).Length,
            RemoteSize     = item.Size
        };

    private static string BuildLocalPath(string localBasePath, string relativePath)
        => localBasePath.CombinePath(relativePath.Replace('/', Path.DirectorySeparatorChar));
}
