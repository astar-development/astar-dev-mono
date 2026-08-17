using AStar.Dev.Infrastructure.AppDb.Domain;

namespace AStarDev.OneDriveSyncClient.Home;

/// <summary>Creates root <see cref="FolderTreeNodeViewModel"/> instances with their service dependencies resolved from the container.</summary>
public interface IFolderTreeNodeViewModelFactory
{
    /// <summary>Creates a root tree node view model for the supplied folder node.</summary>
    FolderTreeNodeViewModel Create(FolderTreeNode node, Func<CancellationToken, Task<string>> tokenFactory, DriveId driveId, Func<string, FolderSyncState?> ruleStateResolver);
}
