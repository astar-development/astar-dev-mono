using AStar.Dev.OneDrive.Sync.Client.Accounts;
using AStar.Dev.OneDrive.Sync.Client.Data.Repositories;
using AStar.Dev.OneDrive.Sync.Client.Domain;
using AStar.Dev.OneDrive.Sync.Client.Infrastructure.Authentication;
using AStar.Dev.OneDrive.Sync.Client.Infrastructure.Rules;
using AStar.Dev.OneDrive.Sync.Client.Home;
using AStar.Dev.OneDrive.Sync.Client.Infrastructure.Graph;
using AStar.Dev.OneDrive.Sync.Client.Infrastructure.Shell;
using AStar.Dev.OneDrive.Sync.Client.Localization;
using AccountId = AStar.Dev.OneDrive.Sync.Client.Data.Entities.AccountId;
using Microsoft.Extensions.Logging;
using AStar.Dev.Functional.Extensions;

namespace AStar.Dev.OneDrive.Sync.Client.Tests.Unit.Accounts;

public sealed class GivenAnAccountFilesViewModelOpeningFileManager
{
    private const string AccountIdString = "account-1";
    private const string AccessToken     = "token-abc";
    private const string DriveIdValue    = "drive-1";
    private const string FolderId        = "folder-1";
    private const string FolderName      = "Photos";

    [Fact]
    public async Task when_directory_exists_then_open_folder_is_called()
    {
        var fileManagerService = Substitute.For<IFileManagerService>();
        var fileSystem = Substitute.For<IFileSystem>();
        fileSystem.Directory.Exists(Arg.Any<string>()).Returns(true);

        var sut = BuildSut(fileManagerService, fileSystem);

        await sut.LoadCommand.ExecuteAsync(null);
        sut.RootFolders[0].OpenInFileManagerCommand.Execute(null);

        fileManagerService.Received(1).OpenFolder(Arg.Any<string>());
    }

    [Fact]
    public async Task when_directory_does_not_exist_then_open_folder_is_not_called()
    {
        var fileManagerService = Substitute.For<IFileManagerService>();
        var fileSystem = Substitute.For<IFileSystem>();
        fileSystem.Directory.Exists(Arg.Any<string>()).Returns(false);

        var sut = BuildSut(fileManagerService, fileSystem);

        await sut.LoadCommand.ExecuteAsync(null);
        sut.RootFolders[0].OpenInFileManagerCommand.Execute(null);

        fileManagerService.DidNotReceive().OpenFolder(Arg.Any<string>());
    }

    private static AccountFilesViewModel BuildSut(IFileManagerService fileManagerService, IFileSystem fileSystem)
    {
        var authService = Substitute.For<IAuthService>();
        var graphService = Substitute.For<IGraphService>();
        var repository = Substitute.For<IAccountRepository>();
        var syncRuleRepo = Substitute.For<ISyncRuleRepository>();

        authService.AcquireTokenSilentAsync(AccountIdString, Arg.Any<CancellationToken>())
            .Returns(AuthResultFactory.Success(AccessToken, AccountIdString, AccountProfileFactory.Create("Test User", "test@test.com")));

        graphService.GetDriveIdAsync(AccountIdString, Arg.Any<Func<CancellationToken, Task<string>>>(), Arg.Any<CancellationToken>())
            .Returns(new Result<DriveId, string>.Ok(new DriveId(DriveIdValue)));

        graphService.GetRootFoldersAsync(AccountIdString, Arg.Any<Func<CancellationToken, Task<string>>>(), Arg.Any<CancellationToken>())
            .Returns(new Result<List<DriveFolder>, string>.Ok([new DriveFolder(FolderId, FolderName, Option.None<string>())]));

        syncRuleRepo.GetByAccountIdAsync(Arg.Any<AccountId>(), Arg.Any<CancellationToken>())
            .Returns([]);

        var account = new OneDriveAccount
        {
            Id      = new AccountId(AccountIdString),
            Profile = AccountProfileFactory.Create("Test User", "test@test.com")
        };

        return new AccountFilesViewModel(account, authService, graphService, new SyncRuleService(syncRuleRepo, Substitute.For<ILogger<SyncRuleService>>()), fileSystem, fileManagerService, Substitute.For<ILogger<AccountFilesViewModel>>(), new FolderTreeNodeViewModelFactory(graphService, Substitute.For<ILogger<FolderTreeNodeViewModel>>(), Substitute.For<ILocalizationService>()), Substitute.For<ILocalizationService>());
    }
}
