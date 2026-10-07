using AStar.Dev.Infrastructure.AppDb.Domain;
using AStarDev.FunctionalParadigm;
using AStarDev.OneDriveSyncClient.Infrastructure.Graph;
using AStarDev.OneDriveSyncClient.Localization;
using Microsoft.Extensions.Logging;

namespace AStarDev.OneDriveSyncClient.Home;

/// <summary>Container-backed factory for root <see cref="FolderTreeNodeViewModel"/> instances.</summary>
public sealed class FolderTreeNodeViewModelFactory(IGraphService graphService, ILogger<FolderTreeNodeViewModel> logger, ILocalizationService localizationService, ILocalFolderLister localFolderLister) : IFolderTreeNodeViewModelFactory
{
    /// <inheritdoc />
    public FolderTreeNodeViewModel Create(FolderTreeNode node, Func<CancellationToken, Task<string>> tokenFactory, DriveId driveId, Func<string, FolderSyncState?> ruleStateResolver, string localSyncRoot) => new(node, graphService, tokenFactory, driveId, ruleStateResolver, remotePath => localFolderLister.ListChildFolderNames(localSyncRoot, remotePath), logger, localizationService);

    /// <inheritdoc />
    public IReadOnlyList<FolderTreeNodeViewModel> CreateRootLevel(IReadOnlyList<DriveFolder> remoteFolders, string accountId, string localSyncRoot, Func<CancellationToken, Task<string>> tokenFactory, DriveId driveId, Func<string, FolderSyncState?> ruleStateResolver)
    {
        var mergedFolders = FolderTreeMerger.Merge(remoteFolders, localFolderLister.ListChildFolderNames(localSyncRoot, string.Empty), string.Empty, Option.None<string>(), FolderSyncState.Excluded, path => ruleStateResolver(path).ToOption());

        return [.. mergedFolders.Select(mergedFolder => Create(new FolderTreeNode(mergedFolder.RemoteId, mergedFolder.Name, mergedFolder.ParentId, accountId, mergedFolder.RemotePath, mergedFolder.SyncState, HasChildren: true), tokenFactory, driveId, ruleStateResolver, localSyncRoot))];
    }
}
