using AStar.Dev.Infrastructure.AppDb.Domain;

namespace AStarDev.OneDriveSyncClient.Home;

/// <summary>Creates root <see cref="FolderTreeNodeViewModel"/> instances with their service dependencies resolved from the container.</summary>
public interface IFolderTreeNodeViewModelFactory
{
    /// <summary>Creates a root tree node view model for the supplied folder node. <paramref name="localSyncRoot"/> is the account's local sync folder used to find local-only child folders.</summary>
    FolderTreeNodeViewModel Create(FolderTreeNode node, Func<CancellationToken, Task<string>> tokenFactory, DriveId driveId, Func<string, FolderSyncState?> ruleStateResolver, string localSyncRoot);

    /// <summary>Merges the remote root folders with the local folders found directly under <paramref name="localSyncRoot"/> and creates a root view model for each.</summary>
    IReadOnlyList<FolderTreeNodeViewModel> CreateRootLevel(IReadOnlyList<DriveFolder> remoteFolders, string accountId, string localSyncRoot, Func<CancellationToken, Task<string>> tokenFactory, DriveId driveId, Func<string, FolderSyncState?> ruleStateResolver);
}
