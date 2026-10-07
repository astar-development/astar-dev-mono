using AStar.Dev.Infrastructure.AppDb.Entities;
using AStarDev.OneDriveSyncClient.Accounts;

namespace AStarDev.OneDriveSyncClient.Infrastructure.Sync.Detection;

/// <summary>Creates, on OneDrive, the folders that are included for sync but only exist locally.</summary>
public interface IRemoteFolderCreator
{
    /// <summary>For each include rule without a remote id whose local folder exists, resolves or creates the remote folder (creating missing parents first) and stores its id on the rule. Failures are logged and skipped.</summary>
    Task CreateMissingFoldersAsync(OneDriveAccount account, AccountSyncConfig syncConfig, Func<CancellationToken, Task<string>> tokenFactory, RemoteEnumerationContext context, CancellationToken cancellationToken = default);
}
