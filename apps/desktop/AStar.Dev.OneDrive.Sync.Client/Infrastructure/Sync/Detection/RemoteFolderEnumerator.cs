using AStar.Dev.Functional.Extensions;
using AStar.Dev.OneDrive.Sync.Client.Accounts;
using AStar.Dev.OneDrive.Sync.Client.Data.Entities;
using AStar.Dev.OneDrive.Sync.Client.Data.Repositories;
using AStar.Dev.OneDrive.Sync.Client.Domain;
using AStar.Dev.OneDrive.Sync.Client.Home;
using AStar.Dev.OneDrive.Sync.Client.Infrastructure.Graph;
using AStar.Dev.OneDrive.Sync.Client.Infrastructure.Logging;
using Microsoft.Extensions.Logging;
using AccountId = AStar.Dev.OneDrive.Sync.Client.Data.Entities.AccountId;

namespace AStar.Dev.OneDrive.Sync.Client.Infrastructure.Sync.Detection;

/// <inheritdoc />
public sealed class RemoteFolderEnumerator(IGraphService graphService, ISyncRuleRepository syncRuleRepository, ISyncedItemRepository syncedItemRepository, ILogger<RemoteFolderEnumerator> logger) : IRemoteFolderEnumerator
{
    /// <inheritdoc />
    public async Task<RemoteEnumerationResult> EnumerateAsync(OneDriveAccount account, Func<CancellationToken, Task<string>> tokenFactory, Action<int>? onItemDiscovered = null, CancellationToken ct = default)
    {
        var rules = await syncRuleRepository.GetByAccountIdAsync(account.Id, ct).ConfigureAwait(false);

        if (rules.Count == 0)
        {
            OneDriveSyncClientMessages.RemoteFolderEnumeratorNoRules(logger, account.Id.Id);

            return new RemoteEnumerationResult([], new HashSet<string>(StringComparer.OrdinalIgnoreCase), [], [], HadNoRules: true);
        }

        var syncedItems = await syncedItemRepository.GetAllByAccountAsync(account.Id, ct).ConfigureAwait(false);
        var driveId = await graphService.GetDriveIdAsync(account.Id.Id, tokenFactory, ct)
            .MatchAsync<DriveId, string, DriveId?>(
                id => id,
                error =>
                {
                    OneDriveSyncClientMessages.RemoteFolderEnumeratorError(logger, error);
                    return null;
                }).ConfigureAwait(false);

        if (driveId is null)
            return new RemoteEnumerationResult([], new HashSet<string>(StringComparer.OrdinalIgnoreCase), [], [], HadNoRules: false);

        var includeRules = rules.Where(r => r.RuleType == RuleType.Include).ToList();
        var rootIncludeRules = includeRules
            .Where(rule => !includeRules.Any(other => other.RemotePath != rule.RemotePath && rule.RemotePath.StartsWith(other.RemotePath + "/", StringComparison.OrdinalIgnoreCase)))
            .ToList();

        List<DeltaItem> allDeltaItems = [];
        var seenRemoteIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var rule in rootIncludeRules)
        {
            if (ct.IsCancellationRequested)
                break;

            string? folderId = await ResolveAndBackFillFolderIdAsync(account.Id, rule, syncedItems, tokenFactory, driveId.Value, ct).ConfigureAwait(false);

            if (folderId is null)
            {
                OneDriveSyncClientMessages.RemoteFolderEnumeratorCannotResolveId(logger, rule.RemotePath);
                continue;
            }

            OneDriveSyncClientMessages.RemoteFolderEnumeratorEnumerating(logger, rule.RemotePath, account.Id.Id);
            var items = await graphService.EnumerateFolderAsync(tokenFactory, driveId.Value, folderId, rule.RemotePath, onItemDiscovered, ct)
                .MatchAsync<List<DeltaItem>, string, List<DeltaItem>?>(
                    deltaItems => deltaItems,
                    error =>
                    {
                        OneDriveSyncClientMessages.RemoteFolderEnumeratorFailed(logger, rule.RemotePath, error);
                        return null;
                    }).ConfigureAwait(false);

            if (items is null)
                continue;

            OneDriveSyncClientMessages.RemoteFolderEnumeratorEnumerated(logger, items.Count, rule.RemotePath);

            foreach (var item in items)
                seenRemoteIds.Add(item.Id.Id);

            allDeltaItems.AddRange(items);
        }

        return new RemoteEnumerationResult(allDeltaItems, seenRemoteIds, syncedItems, rules);
    }

    private async Task<string?> ResolveAndBackFillFolderIdAsync(AccountId accountId, SyncRuleEntity rule, Dictionary<string, SyncedItemEntity> syncedItems, Func<CancellationToken, Task<string>> tokenFactory, DriveId driveId, CancellationToken ct)
    {
        string? folderId = rule.RemoteItemId is Option<string>.Some existingId
            ? existingId.Value
            : TryResolveFromSyncedItems(syncedItems, rule.RemotePath)
                ?? await graphService.GetFolderIdByPathAsync(tokenFactory, driveId, rule.RemotePath, ct).ConfigureAwait(false);

        if (folderId is not null && rule.RemoteItemId.Match(id => id != folderId, () => true))
        {
            OneDriveSyncClientMessages.RemoteFolderEnumeratorBackfilling(logger, rule.RemotePath);
            await syncRuleRepository.UpsertAsync(accountId, rule.RemotePath, RuleType.Include, folderId, ct).ConfigureAwait(false);
        }

        return folderId;
    }

    private static string? TryResolveFromSyncedItems(Dictionary<string, SyncedItemEntity> syncedItems, string remotePath)
        => syncedItems.Values.FirstOrDefault(i => i.IsFolder && string.Equals(i.RemotePath, remotePath, StringComparison.OrdinalIgnoreCase))?.RemoteItemId.Id;
}
