using System.Runtime.CompilerServices;
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
    public async IAsyncEnumerable<DeltaItem> StreamAsync(OneDriveAccount account, Func<CancellationToken, Task<string>> tokenFactory, RemoteEnumerationContext context, Action<int>? onItemDiscovered = null, [EnumeratorCancellation] CancellationToken ct = default)
    {
        var rules = await syncRuleRepository.GetByAccountIdAsync(account.Id, ct).ConfigureAwait(false);

        if (rules.Count == 0)
        {
            OneDriveSyncClientMessages.RemoteFolderEnumeratorNoRules(logger, account.Id.Id);
            context.HadNoRules = true;
            yield break;
        }

        context.Rules = rules;
        context.SyncedItems = await syncedItemRepository.GetAllByAccountAsync(account.Id, ct).ConfigureAwait(false);

        var driveId = await graphService.GetDriveIdAsync(account.Id.Id, tokenFactory, ct)
            .MatchAsync<DriveId, string, DriveId?>(
                driveIdValue => driveIdValue,
                error =>
                {
                    OneDriveSyncClientMessages.RemoteFolderEnumeratorError(logger, error);
                    return null;
                }).ConfigureAwait(false);

        if (driveId is null)
            yield break;

        var includeRules = rules.Where(r => r.RuleType == RuleType.Include).ToList();
        var rootIncludeRules = includeRules
            .Where(rule => !includeRules.Any(other => other.RemotePath != rule.RemotePath && rule.RemotePath.StartsWith(other.RemotePath + "/", StringComparison.OrdinalIgnoreCase)))
            .ToList();

        foreach (var rule in rootIncludeRules)
        {
            if (ct.IsCancellationRequested)
                yield break;

            string? folderId = await ResolveAndBackFillFolderIdAsync(account.Id, rule, context.SyncedItems, tokenFactory, driveId.Value, ct).ConfigureAwait(false);

            if (folderId is null)
            {
                OneDriveSyncClientMessages.RemoteFolderEnumeratorCannotResolveId(logger, rule.RemotePath);
                continue;
            }

            OneDriveSyncClientMessages.RemoteFolderEnumeratorEnumerating(logger, rule.RemotePath, account.Id.Id);
            var folderEnumerator = graphService.EnumerateFolderAsync(tokenFactory, driveId.Value, folderId, rule.RemotePath, onItemDiscovered, ct).GetAsyncEnumerator(ct);
            int itemCount = 0;

            try
            {
                while (true)
                {
                    bool hasNext;
                    try
                    {
                        hasNext = await folderEnumerator.MoveNextAsync().ConfigureAwait(false);
                    }
                    catch (Exception ex) when (ex is not OperationCanceledException and not SyncReAuthRequiredException)
                    {
                        OneDriveSyncClientMessages.RemoteFolderEnumeratorFailed(logger, rule.RemotePath, ex.Message);
                        break;
                    }

                    if (!hasNext)
                        break;

                    itemCount++;
                    context.SeenRemoteIds.Add(folderEnumerator.Current.Id.Id);
                    yield return folderEnumerator.Current;
                }
            }
            finally
            {
                await folderEnumerator.DisposeAsync().ConfigureAwait(false);
                OneDriveSyncClientMessages.RemoteFolderEnumeratorEnumerated(logger, itemCount, rule.RemotePath);
            }
        }
    }

    private async Task<string?> ResolveAndBackFillFolderIdAsync(AccountId accountId, SyncRuleEntity rule, Dictionary<string, SyncedItemEntity> syncedItems, Func<CancellationToken, Task<string>> tokenFactory, DriveId driveId, CancellationToken ct)
    {
        string? folderId = rule.RemoteItemId is Option<string>.Some existingId
            ? existingId.Value
            : TryResolveFromSyncedItems(syncedItems, rule.RemotePath)
                ?? await graphService.GetFolderIdByPathAsync(tokenFactory, driveId, rule.RemotePath, ct).ConfigureAwait(false);

        if (folderId is not null && rule.RemoteItemId.Match(resolvedId => resolvedId != folderId, () => true))
        {
            OneDriveSyncClientMessages.RemoteFolderEnumeratorBackfilling(logger, rule.RemotePath);
            await syncRuleRepository.UpsertAsync(accountId, rule.RemotePath, RuleType.Include, folderId, ct).ConfigureAwait(false);
        }

        return folderId;
    }

    private static string? TryResolveFromSyncedItems(Dictionary<string, SyncedItemEntity> syncedItems, string remotePath)
        => syncedItems.Values.FirstOrDefault(i => i.IsFolder && string.Equals(i.RemotePath, remotePath, StringComparison.OrdinalIgnoreCase))?.RemoteItemId.Id;
}
