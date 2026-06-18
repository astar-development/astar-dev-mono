using AStar.Dev.OneDrive.Sync.Client.Infrastructure.Graph;
using AStar.Dev.OneDrive.Sync.Client.Localization;
using Microsoft.Extensions.Logging;

namespace AStar.Dev.OneDrive.Sync.Client.Home;

/// <summary>Container-backed factory for root <see cref="FolderTreeNodeViewModel"/> instances.</summary>
public sealed class FolderTreeNodeViewModelFactory(IGraphService graphService, ILogger<FolderTreeNodeViewModel> logger, ILocalizationService localizationService) : IFolderTreeNodeViewModelFactory
{
    /// <inheritdoc />
    public FolderTreeNodeViewModel Create(FolderTreeNode node, Func<CancellationToken, Task<string>> tokenFactory, DriveId driveId, Func<string, FolderSyncState?> ruleStateResolver) => new(node, graphService, tokenFactory, driveId, ruleStateResolver, logger, localizationService);
}
