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

public sealed class GivenAnAccountFilesViewModelFolderCountChanged
{
    private const string AccountIdString = "account-1";
    private const string AccessToken = "token-abc";
    private const string DriveIdValue = "drive-1";
    private const string FolderId = "folder-1";
    private const string FolderName = "Photos";

    [Fact]
    public async Task when_a_folder_is_toggled_included_then_folder_count_changed_event_is_raised_with_include_rule_count()
    {
        int? capturedCount = null;
        int callCount = 0;
        var syncRuleRepo = Substitute.For<ISyncRuleRepository>();
        syncRuleRepo.GetByAccountIdAsync(Arg.Any<AccountId>(), Arg.Any<CancellationToken>())
            .Returns(_ => Task.FromResult(callCount++ == 0
                ? new List<SyncRuleEntity>()
                : [new SyncRuleEntity { AccountId = new AccountId(AccountIdString), RemotePath = $"/{FolderName}", RuleType = RuleType.Include }]));

        var sut = BuildSut(syncRuleRepo);
        sut.FolderCountChanged += (_, count) => capturedCount = count;

        await sut.LoadCommand.ExecuteAsync(null);
        sut.RootFolders[0].ToggleIncludeCommand.Execute(null);

        capturedCount.ShouldBe(1);
    }

    [Fact]
    public async Task when_a_folder_is_toggled_excluded_then_folder_count_changed_event_is_raised_with_zero_count()
    {
        int? capturedCount = null;
        int callCount = 0;
        var syncRuleRepo = Substitute.For<ISyncRuleRepository>();
        syncRuleRepo.GetByAccountIdAsync(Arg.Any<AccountId>(), Arg.Any<CancellationToken>())
            .Returns(_ => Task.FromResult(callCount++ == 0
                ? [new SyncRuleEntity { AccountId = new AccountId(AccountIdString), RemotePath = $"/{FolderName}", RuleType = RuleType.Include }]
                : new List<SyncRuleEntity>()));

        var sut = BuildSut(syncRuleRepo);
        sut.FolderCountChanged += (_, count) => capturedCount = count;

        await sut.LoadCommand.ExecuteAsync(null);
        sut.RootFolders[0].ToggleIncludeCommand.Execute(null);

        capturedCount.ShouldBe(0);
    }

    private static AccountFilesViewModel BuildSut(ISyncRuleRepository syncRuleRepo)
    {
        var authService = Substitute.For<IAuthService>();
        var graphService = Substitute.For<IGraphService>();
        var repository = Substitute.For<IAccountRepository>();

        authService.AcquireTokenSilentAsync(AccountIdString, Arg.Any<CancellationToken>())
            .Returns(AuthResultFactory.Success(AccessToken, AccountIdString, AccountProfileFactory.Create("Test User", "test@test.com")));

        graphService.GetDriveIdAsync(Arg.Any<string>(), Arg.Any<Func<CancellationToken, Task<string>>>(), Arg.Any<CancellationToken>())
            .Returns(new Ok<DriveId, string>(new DriveId(DriveIdValue)));

        graphService.GetRootFoldersAsync(Arg.Any<string>(), Arg.Any<Func<CancellationToken, Task<string>>>(), Arg.Any<CancellationToken>())
            .Returns(new Ok<List<DriveFolder>, string>([new DriveFolder(FolderId, FolderName, Option.None<string>())]));
        var fileSystemServices = new FileSystemServices(Substitute.For<IFileSystem>(), Substitute.For<IFileManagerService>());

        var accountFilesViewServices = new AccountFilesViewServices(authService, Substitute.For<ILocalizationService>(), graphService, new SyncRuleService(syncRuleRepo, Substitute.For<ILogger<SyncRuleService>>()));
        return new AccountFilesViewModel(BuildAccount(), accountFilesViewServices, fileSystemServices, Substitute.For<ILogger<AccountFilesViewModel>>(), new FolderTreeNodeViewModelFactory(graphService, Substitute.For<ILogger<FolderTreeNodeViewModel>>(), Substitute.For<ILocalizationService>()));
    }

    private static OneDriveAccount BuildAccount()
        => new()
        {
            Id = new AccountId(AccountIdString),
            Profile = AccountProfileFactory.Create("Test User", "test@test.com"),
            SyncConfig = Option.None<AccountSyncConfig>()
        };
}
