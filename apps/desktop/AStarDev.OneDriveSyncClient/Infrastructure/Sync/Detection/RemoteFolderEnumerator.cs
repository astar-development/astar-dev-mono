using System.Collections.Concurrent;
using System.Runtime.CompilerServices;
using AStar.Dev.FunctionalParadigm;
using AStar.Dev.Infrastructure.AppDb.Domain;
using AStar.Dev.Infrastructure.AppDb.Entities;
using AStarDev.OneDriveSyncClient.Accounts;
using AStarDev.OneDriveSyncClient.Data.Repositories;
using AStarDev.OneDriveSyncClient.Infrastructure.Graph;
using AStarDev.OneDriveSyncClient.Infrastructure.Logging;
using Microsoft.Extensions.Logging;
using AccountId = AStar.Dev.Infrastructure.AppDb.Entities.AccountId;

namespace AStarDev.OneDriveSyncClient.Infrastructure.Sync.Detection;

/// <inheritdoc />
public sealed class RemoteFolderEnumerator(IGraphService graphService, ISyncRuleRepository syncRuleRepository, ISyncedItemRepository syncedItemRepository, ILogger<RemoteFolderEnumerator> logger) : IRemoteFolderEnumerator
{
    /// <inheritdoc />
    public async IAsyncEnumerable<DeltaItem> StreamAsync(OneDriveAccount account, Func<CancellationToken, Task<string>> tokenFactory, RemoteEnumerationContext context, Action<int>? onItemDiscovered = null, Action<string>? onStageChanged = null, [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        var rules = await syncRuleRepository.GetByAccountIdAsync(account.Id, cancellationToken).ConfigureAwait(false);

        if (rules.Count == 0)
        {
            OneDriveSyncClientMessages.RemoteFolderEnumeratorNoRules(logger, account.Id.Value);
            context.HadNoRules = true;
            yield break;
        }

        context.Rules = rules;
        context.SyncedItems = new ConcurrentDictionary<string, SyncedItemEntity>(await syncedItemRepository.GetAllByAccountAsync(account.Id, cancellationToken).ConfigureAwait(false), StringComparer.OrdinalIgnoreCase);

        onStageChanged?.Invoke("Sync.ConnectingToDrive");
        OneDriveSyncClientMessages.RemoteFolderEnumeratorConnectingToDrive(logger, account.Id.Value);

        var driveId = await graphService.GetDriveIdAsync(account.Id.Value, tokenFactory, cancellationToken)
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
            if (cancellationToken.IsCancellationRequested)
                yield break;

            onStageChanged?.Invoke("Sync.ResolvingFolder");
            OneDriveSyncClientMessages.RemoteFolderEnumeratorResolvingFolder(logger, rule.RemotePath);

            string? folderId = await ResolveAndBackFillFolderIdAsync(account.Id, rule, context.SyncedItems, tokenFactory, driveId.Value, cancellationToken).ConfigureAwait(false);

            if (folderId is null)
            {
                OneDriveSyncClientMessages.RemoteFolderEnumeratorCannotResolveId(logger, rule.RemotePath);
                continue;
            }

            OneDriveSyncClientMessages.RemoteFolderEnumeratorEnumerating(logger, rule.RemotePath, account.Id.Value);
            var folderEnumerator = graphService.EnumerateFolderAsync(tokenFactory, driveId.Value, folderId, rule.RemotePath, onItemDiscovered, cancellationToken).GetAsyncEnumerator(cancellationToken);
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
                    context.SeenRemoteIds.Add(folderEnumerator.Current.Id.Value);
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

    private async Task<string?> ResolveAndBackFillFolderIdAsync(AccountId accountId, SyncRuleEntity rule, ConcurrentDictionary<string, SyncedItemEntity> syncedItems, Func<CancellationToken, Task<string>> tokenFactory, DriveId driveId, CancellationToken cancellationToken)
    {
        string? folderId = rule.RemoteItemId is Option<string>.Some existingId
            ? existingId.Value
            : TryResolveFromSyncedItems(syncedItems, rule.RemotePath)
                ?? await graphService.GetFolderIdByPathAsync(tokenFactory, driveId, rule.RemotePath, cancellationToken).ConfigureAwait(false);

        if (folderId is not null && rule.RemoteItemId.Match(resolvedId => resolvedId != folderId, () => true))
        {
            OneDriveSyncClientMessages.RemoteFolderEnumeratorBackfilling(logger, rule.RemotePath);
            await syncRuleRepository.UpsertAsync(accountId, rule.RemotePath, RuleType.Include, folderId, cancellationToken).ConfigureAwait(false);
        }

        return folderId;
    }

    private static string? TryResolveFromSyncedItems(ConcurrentDictionary<string, SyncedItemEntity> syncedItems, string remotePath)
        => syncedItems.Values.FirstOrDefault(i => i.IsFolder && string.Equals(i.RemotePath, remotePath, StringComparison.OrdinalIgnoreCase))?.RemoteItemId.Value;
}
