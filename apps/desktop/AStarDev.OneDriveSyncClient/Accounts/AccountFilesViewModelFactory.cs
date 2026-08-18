using AStarDev.OneDriveSyncClient.Home;
using AStarDev.OneDriveSyncClient.Infrastructure.Shell;
using Microsoft.Extensions.Logging;

namespace AStarDev.OneDriveSyncClient.Accounts;

/// <summary>Container-backed factory for <see cref="AccountFilesViewModel"/> instances.</summary>
public sealed class AccountFilesViewModelFactory(IAccountFilesViewServices accountFilesViewServices, FileSystemServices fileSystemServices, ILogger<AccountFilesViewModel> logger, IFolderTreeNodeViewModelFactory folderTreeNodeViewModelFactory) : IAccountFilesViewModelFactory
{
    /// <inheritdoc />
    public AccountFilesViewModel Create(OneDriveAccount account) => new(account, accountFilesViewServices, fileSystemServices, logger, folderTreeNodeViewModelFactory);
}
