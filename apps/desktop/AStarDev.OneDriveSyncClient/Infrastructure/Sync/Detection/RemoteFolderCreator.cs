using System.Collections.Concurrent;
using System.IO.Abstractions;
using AStar.Dev.Infrastructure.AppDb.Domain;
using AStar.Dev.Infrastructure.AppDb.Entities;
using AStarDev.FunctionalParadigm;
using AStarDev.OneDriveSyncClient.Accounts;
using AStarDev.OneDriveSyncClient.Data.Repositories;
using AStarDev.OneDriveSyncClient.Infrastructure.Graph;
using AStarDev.OneDriveSyncClient.Infrastructure.Logging;
using AStarDev.Utilities;
using Microsoft.Extensions.Logging;

namespace AStarDev.OneDriveSyncClient.Infrastructure.Sync.Detection;

/// <inheritdoc />
public sealed class RemoteFolderCreator(IGraphService graphService, ISyncRuleRepository syncRuleRepository, IFileSystem fileSystem, ILogger<RemoteFolderCreator> logger) : IRemoteFolderCreator
{
    private const string DriveRootId = "root";

    /// <inheritdoc />
    public async Task CreateMissingFoldersAsync(OneDriveAccount account, AccountSyncConfig syncConfig, Func<CancellationToken, Task<string>> tokenFactory, RemoteEnumerationContext context, CancellationToken cancellationToken = default)
    {
        var candidates = context.Rules
            .Where(rule => rule.RuleType == RuleType.Include && rule.RemoteItemId.Match(_ => false, () => true))
            .Where(rule => !IsTracked(context.SyncedItems, rule.RemotePath) && LocalFolderExists(syncConfig, rule.RemotePath))
            .OrderBy(rule => rule.RemotePath.Length)
            .ToList();

        if (candidates.Count == 0)
            return;

        var driveId = await graphService.GetDriveIdAsync(account.Id.Value, tokenFactory, cancellationToken)
            .MatchAsync<DriveId, string, DriveId?>(
                id => id,
                error =>
                {
                    OneDriveSyncClientMessages.RemoteFolderCreatorDriveUnavailable(logger, account.Id.Value, error);

                    return null;
                }).ConfigureAwait(false);

        if (driveId is null)
            return;

        foreach (var rule in candidates)
            await EnsureRuleFolderAsync(account, tokenFactory, driveId.Value, rule, cancellationToken).ConfigureAwait(false);
    }

    private async Task EnsureRuleFolderAsync(OneDriveAccount account, Func<CancellationToken, Task<string>> tokenFactory, DriveId driveId, SyncRuleEntity rule, CancellationToken cancellationToken)
    {
        var result = await EnsureFolderAsync(account.Id.Value, tokenFactory, driveId, rule.RemotePath, cancellationToken).ConfigureAwait(false);

        _ = await result.MatchAsync(
            async folderId =>
            {
                await syncRuleRepository.UpsertAsync(account.Id, rule.RemotePath, RuleType.Include, folderId, cancellationToken).ConfigureAwait(false);
                OneDriveSyncClientMessages.RemoteFolderCreated(logger, rule.RemotePath);

                return true;
            },
            error =>
            {
                OneDriveSyncClientMessages.RemoteFolderCreateFailed(logger, rule.RemotePath, error);

                return false;
            }).ConfigureAwait(false);
    }

    private async Task<Result<string, string>> EnsureFolderAsync(string accountId, Func<CancellationToken, Task<string>> tokenFactory, DriveId driveId, string remotePath, CancellationToken cancellationToken)
    {
        string? existingId = await graphService.GetFolderIdByPathAsync(tokenFactory, driveId, remotePath, cancellationToken).ConfigureAwait(false);

        if (existingId is not null)
            return new Ok<string, string>(existingId);

        int separatorIndex = remotePath.LastIndexOf('/');
        string parentPath = remotePath[..separatorIndex];
        string folderName = remotePath[(separatorIndex + 1)..];
        var parentResult = parentPath.Length == 0
            ? new Ok<string, string>(DriveRootId)
            : await EnsureFolderAsync(accountId, tokenFactory, driveId, parentPath, cancellationToken).ConfigureAwait(false);

        return await parentResult.MatchAsync(
            async parentId =>
            {
                var created = await graphService.CreateFolderAsync(accountId, tokenFactory, parentId, folderName, cancellationToken).ConfigureAwait(false);

                return created.Match(
                    folder => (Result<string, string>)new Ok<string, string>(folder.Id),
                    error => new Fail<string, string>(error));
            },
            error => new Fail<string, string>(error)).ConfigureAwait(false);
    }

    private static bool IsTracked(ConcurrentDictionary<string, SyncedItemEntity> syncedItems, string remotePath)
        => syncedItems.Values.Any(item => item.IsFolder && string.Equals(item.RemotePath, remotePath, StringComparison.OrdinalIgnoreCase));

    private bool LocalFolderExists(AccountSyncConfig syncConfig, string remotePath)
        => fileSystem.Directory.Exists(syncConfig.LocalSyncPath.Value.CombinePath(remotePath.TrimStart('/')));
}
