using AStar.Dev.FunctionalParadigm;
using AStarDev.OneDriveSyncClient.Accounts;
using AStarDev.OneDriveSyncClient.Data.Repositories;
using AStarDev.OneDriveSyncClient.Home;
using AStarDev.OneDriveSyncClient.Infrastructure.Authentication;
using AStarDev.OneDriveSyncClient.Infrastructure.Graph;
using AStarDev.OneDriveSyncClient.Infrastructure.Rules;
using AStarDev.OneDriveSyncClient.Infrastructure.Shell;
using AStarDev.OneDriveSyncClient.Localization;
using Microsoft.Extensions.Logging;
using AccountId = AStar.Dev.Infrastructure.AppDb.Entities.AccountId;

namespace AStarDev.OneDriveSyncClient.TestsUnit.Accounts;

public sealed class GivenAnAccountFilesViewModelOpeningFileManager
{
    private const string AccountIdString = "account-1";
    private const string AccessToken = "token-abc";
    private const string DriveIdValue = "drive-1";
    private const string FolderId = "folder-1";
    private const string FolderName = "Photos";

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
            .Returns(new Ok<DriveId, string>(new DriveId(DriveIdValue)));

        graphService.GetRootFoldersAsync(AccountIdString, Arg.Any<Func<CancellationToken, Task<string>>>(), Arg.Any<CancellationToken>())
            .Returns(new Ok<List<DriveFolder>, string>([new DriveFolder(FolderId, FolderName, Option.None<string>())]));

        syncRuleRepo.GetByAccountIdAsync(Arg.Any<AccountId>(), Arg.Any<CancellationToken>())
            .Returns([]);

        var account = new OneDriveAccount
        {
            Id = new AccountId(AccountIdString),
            Profile = AccountProfileFactory.Create("Test User", "test@test.com")
        };
        var fileSystemServices = new FileSystemServices(fileSystem, fileManagerService);

        var accountFilesViewServices = new AccountFilesViewServices(authService, Substitute.For<ILocalizationService>(), graphService, new SyncRuleService(syncRuleRepo, Substitute.For<ILogger<SyncRuleService>>()));
        return new AccountFilesViewModel(account, accountFilesViewServices, fileSystemServices, Substitute.For<ILogger<AccountFilesViewModel>>(), new FolderTreeNodeViewModelFactory(graphService, Substitute.For<ILogger<FolderTreeNodeViewModel>>(), Substitute.For<ILocalizationService>()));
    }
}
