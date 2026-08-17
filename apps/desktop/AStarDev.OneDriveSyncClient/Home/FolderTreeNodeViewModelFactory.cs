using AStar.Dev.Infrastructure.AppDb.Domain;
using AStarDev.OneDriveSyncClient.Infrastructure.Graph;
using AStarDev.OneDriveSyncClient.Localization;
using Microsoft.Extensions.Logging;

namespace AStarDev.OneDriveSyncClient.Home;

/// <summary>Container-backed factory for root <see cref="FolderTreeNodeViewModel"/> instances.</summary>
public sealed class FolderTreeNodeViewModelFactory(IGraphService graphService, ILogger<FolderTreeNodeViewModel> logger, ILocalizationService localizationService) : IFolderTreeNodeViewModelFactory
{
    /// <inheritdoc />
    public FolderTreeNodeViewModel Create(FolderTreeNode node, Func<CancellationToken, Task<string>> tokenFactory, DriveId driveId, Func<string, FolderSyncState?> ruleStateResolver) => new(node, graphService, tokenFactory, driveId, ruleStateResolver, logger, localizationService);
}
