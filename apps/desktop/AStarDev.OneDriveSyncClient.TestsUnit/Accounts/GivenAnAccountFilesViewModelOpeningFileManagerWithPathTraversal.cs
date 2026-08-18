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

public sealed class GivenAnAccountFilesViewModelOpeningFileManagerWithPathTraversal
{
    private const string AccountIdString = "account-1";
    private const string AccessToken = "token-abc";
    private const string DriveIdValue = "drive-1";
    private const string FolderId = "folder-1";

    [Theory]
    [InlineData("../../etc")]
    [InlineData("../secret")]
    [InlineData("subdir/../../secret")]
    public async Task when_folder_name_contains_parent_traversal_then_file_manager_is_not_launched(string maliciousFolderName)
    {
        var fileManagerService = Substitute.For<IFileManagerService>();
        var fileSystem = Substitute.For<IFileSystem>();
        fileSystem.Directory.Exists(Arg.Any<string>()).Returns(true);

        var sut = BuildSut(fileManagerService, fileSystem, maliciousFolderName);

        await sut.LoadCommand.ExecuteAsync(null);
        sut.RootFolders[0].OpenInFileManagerCommand.Execute(null);

        fileManagerService.DidNotReceive().OpenFolder(Arg.Any<string>());
    }

    [Theory]
    [InlineData("/etc/passwd")]
    [InlineData("/root/.ssh")]
    public async Task when_folder_name_is_absolute_path_escape_then_file_manager_is_not_launched(string absoluteEscapePath)
    {
        var fileManagerService = Substitute.For<IFileManagerService>();
        var fileSystem = Substitute.For<IFileSystem>();
        fileSystem.Directory.Exists(Arg.Any<string>()).Returns(true);

        var sut = BuildSut(fileManagerService, fileSystem, absoluteEscapePath);

        await sut.LoadCommand.ExecuteAsync(null);
        sut.RootFolders[0].OpenInFileManagerCommand.Execute(null);

        fileManagerService.DidNotReceive().OpenFolder(Arg.Any<string>());
    }

    [Fact]
    public async Task when_folder_name_is_legitimate_then_resolved_path_is_under_onedrive_base()
    {
        string capturedPath = string.Empty;
        var fileManagerService = Substitute.For<IFileManagerService>();
        fileManagerService.When(s => s.OpenFolder(Arg.Any<string>()))
            .Do(call => capturedPath = call.Arg<string>());

        var fileSystem = Substitute.For<IFileSystem>();
        fileSystem.Directory.Exists(Arg.Any<string>()).Returns(true);

        string oneDriveBase = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "OneDrive");

        var sut = BuildSut(fileManagerService, fileSystem, "Photos");

        await sut.LoadCommand.ExecuteAsync(null);
        sut.RootFolders[0].OpenInFileManagerCommand.Execute(null);

        fileManagerService.Received(1).OpenFolder(Arg.Any<string>());
        capturedPath.ShouldStartWith(oneDriveBase, Case.Sensitive);
    }

    private static AccountFilesViewModel BuildSut(IFileManagerService fileManagerService, IFileSystem fileSystem, string folderName)
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
            .Returns(new Ok<List<DriveFolder>, string>([new DriveFolder(FolderId, folderName, Option.None<string>())]));

        syncRuleRepo.GetByAccountIdAsync(Arg.Any<AccountId>(), Arg.Any<CancellationToken>())
            .Returns([]);

        var account = new OneDriveAccount
        {
            Id = new AccountId(AccountIdString),
            Profile = AccountProfileFactory.Create("Test User", "test@test.com")
        };
        var fileSystemServices = new FileSystemServices(fileSystem, fileManagerService);

        return new AccountFilesViewModel(account, authService, graphService, new SyncRuleService(syncRuleRepo, Substitute.For<ILogger<SyncRuleService>>()), fileSystemServices, Substitute.For<ILogger<AccountFilesViewModel>>(), new FolderTreeNodeViewModelFactory(graphService, Substitute.For<ILogger<FolderTreeNodeViewModel>>(), Substitute.For<ILocalizationService>()), Substitute.For<ILocalizationService>());
    }
}
