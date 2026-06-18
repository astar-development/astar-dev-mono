using System.IO.Abstractions;
using AStar.Dev.OneDrive.Sync.Client.Home;
using AStar.Dev.OneDrive.Sync.Client.Infrastructure.Authentication;
using AStar.Dev.OneDrive.Sync.Client.Infrastructure.Graph;
using AStar.Dev.OneDrive.Sync.Client.Infrastructure.Rules;
using AStar.Dev.OneDrive.Sync.Client.Infrastructure.Shell;
using AStar.Dev.OneDrive.Sync.Client.Localization;
using Microsoft.Extensions.Logging;

namespace AStar.Dev.OneDrive.Sync.Client.Accounts;

/// <summary>Container-backed factory for <see cref="AccountFilesViewModel"/> instances.</summary>
public sealed class AccountFilesViewModelFactory(IAuthService authService, IGraphService graphService, ISyncRuleService syncRuleService, IFileSystem fileSystem, IFileManagerService fileManagerService, ILogger<AccountFilesViewModel> logger, IFolderTreeNodeViewModelFactory folderTreeNodeViewModelFactory, ILocalizationService localizationService) : IAccountFilesViewModelFactory
{
    /// <inheritdoc />
    public AccountFilesViewModel Create(OneDriveAccount account) => new(account, authService, graphService, syncRuleService, fileSystem, fileManagerService, logger, folderTreeNodeViewModelFactory, localizationService);
}
