using System.IO.Abstractions;
using AStarDev.OneDriveSyncClient.Home;
using AStarDev.OneDriveSyncClient.Infrastructure.Authentication;
using AStarDev.OneDriveSyncClient.Infrastructure.Graph;
using AStarDev.OneDriveSyncClient.Infrastructure.Rules;
using AStarDev.OneDriveSyncClient.Infrastructure.Shell;
using AStarDev.OneDriveSyncClient.Localization;
using Microsoft.Extensions.Logging;

namespace AStarDev.OneDriveSyncClient.Accounts;

/// <summary>Container-backed factory for <see cref="AccountFilesViewModel"/> instances.</summary>
public sealed class AccountFilesViewModelFactory(IAuthService authService, IGraphService graphService, ISyncRuleService syncRuleService, IFileSystem fileSystem, IFileManagerService fileManagerService, ILogger<AccountFilesViewModel> logger, IFolderTreeNodeViewModelFactory folderTreeNodeViewModelFactory, ILocalizationService localizationService) : IAccountFilesViewModelFactory
{
    /// <inheritdoc />
    public AccountFilesViewModel Create(OneDriveAccount account) => new(account, authService, graphService, syncRuleService, fileSystem, fileManagerService, logger, folderTreeNodeViewModelFactory, localizationService);
}
