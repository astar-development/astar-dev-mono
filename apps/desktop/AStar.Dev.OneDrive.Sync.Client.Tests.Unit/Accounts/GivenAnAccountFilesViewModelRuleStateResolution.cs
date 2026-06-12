using System.IO.Abstractions;
using AStar.Dev.Functional.Extensions;
using AStar.Dev.OneDrive.Sync.Client.Accounts;
using AStar.Dev.OneDrive.Sync.Client.Data.Repositories;
using AStar.Dev.OneDrive.Sync.Client.Domain;
using AStar.Dev.OneDrive.Sync.Client.Home;
using AStar.Dev.OneDrive.Sync.Client.Infrastructure.Authentication;
using AStar.Dev.OneDrive.Sync.Client.Infrastructure.Rules;
using AStar.Dev.OneDrive.Sync.Client.Infrastructure.Graph;
using AStar.Dev.OneDrive.Sync.Client.Infrastructure.Shell;
using AStar.Dev.OneDrive.Sync.Client.Localization;
using Microsoft.Extensions.Logging;
using AccountId = AStar.Dev.OneDrive.Sync.Client.Data.Entities.AccountId;

namespace AStar.Dev.OneDrive.Sync.Client.Tests.Unit.Accounts;

public sealed class GivenAnAccountFilesViewModelRuleStateResolution
{
    private const string AccountIdString     = "account-1";
    private const string LocalSyncPathString = "/configured/sync/path";
    private const string AccessToken         = "token-abc";
    private const string DriveIdValue        = "drive-1";
    private const string RootFolderId        = "folder-root";
    private const string RootFolderName      = "Photos";
    private const string ChildFolderId       = "folder-child";
    private const string ChildFolderName     = "Holidays";

    [Fact]
    public async Task when_root_folder_has_exclude_rule_then_its_sync_state_is_excluded_after_load()
    {
        var syncRuleRepo = Substitute.For<ISyncRuleRepository>();
        syncRuleRepo.GetByAccountIdAsync(Arg.Any<AccountId>(), Arg.Any<CancellationToken>())
            .Returns([new SyncRuleEntity { AccountId = new AccountId(AccountIdString), RemotePath = $"/{RootFolderName}", RuleType = RuleType.Exclude }]);

        var sut = BuildSut(BuildMocks(), syncRuleRepo);

        await sut.LoadCommand.ExecuteAsync(null);

        sut.RootFolders[0].SyncState.ShouldBe(FolderSyncState.Excluded);
    }

    [Fact]
    public async Task when_root_folder_has_include_rule_then_its_sync_state_is_included_after_load()
    {
        var syncRuleRepo = Substitute.For<ISyncRuleRepository>();
        syncRuleRepo.GetByAccountIdAsync(Arg.Any<AccountId>(), Arg.Any<CancellationToken>())
            .Returns([new SyncRuleEntity { AccountId = new AccountId(AccountIdString), RemotePath = $"/{RootFolderName}", RuleType = RuleType.Include }]);

        var sut = BuildSut(BuildMocks(), syncRuleRepo);

        await sut.LoadCommand.ExecuteAsync(null);

        sut.RootFolders[0].SyncState.ShouldBe(FolderSyncState.Included);
    }

    [Fact]
    public async Task when_root_folder_has_no_rule_then_its_sync_state_defaults_to_excluded_after_load()
    {
        var syncRuleRepo = Substitute.For<ISyncRuleRepository>();
        syncRuleRepo.GetByAccountIdAsync(Arg.Any<AccountId>(), Arg.Any<CancellationToken>())
            .Returns([]);

        var sut = BuildSut(BuildMocks(), syncRuleRepo);

        await sut.LoadCommand.ExecuteAsync(null);

        sut.RootFolders[0].SyncState.ShouldBe(FolderSyncState.Excluded);
    }

    [Fact]
    public async Task when_parent_is_toggled_included_then_previously_excluded_child_is_included_when_expanded()
    {
        var syncRuleRepo = Substitute.For<ISyncRuleRepository>();
        syncRuleRepo.GetByAccountIdAsync(Arg.Any<AccountId>(), Arg.Any<CancellationToken>())
            .Returns([new SyncRuleEntity { AccountId = new AccountId(AccountIdString), RemotePath = $"/{RootFolderName}/{ChildFolderName}", RuleType = RuleType.Exclude }]);

        var mocks = BuildMocksWithChild();
        var sut = BuildSut(mocks, syncRuleRepo);

        await sut.LoadCommand.ExecuteAsync(null);
        sut.RootFolders[0].ToggleIncludeCommand.Execute(null);
        await sut.RootFolders[0].ToggleExpandCommand.ExecuteAsync(null);

        sut.RootFolders[0].Children[0].SyncState.ShouldBe(FolderSyncState.Included);
    }

    [Fact]
    public async Task when_parent_is_toggled_excluded_then_previously_included_child_is_excluded_when_expanded()
    {
        var syncRuleRepo = Substitute.For<ISyncRuleRepository>();
        syncRuleRepo.GetByAccountIdAsync(Arg.Any<AccountId>(), Arg.Any<CancellationToken>())
            .Returns([
                new SyncRuleEntity { AccountId = new AccountId(AccountIdString), RemotePath = $"/{RootFolderName}", RuleType = RuleType.Include },
                new SyncRuleEntity { AccountId = new AccountId(AccountIdString), RemotePath = $"/{RootFolderName}/{ChildFolderName}", RuleType = RuleType.Include }
            ]);

        var mocks = BuildMocksWithChild();
        var sut = BuildSut(mocks, syncRuleRepo);

        await sut.LoadCommand.ExecuteAsync(null);
        sut.RootFolders[0].ToggleIncludeCommand.Execute(null);
        await sut.RootFolders[0].ToggleExpandCommand.ExecuteAsync(null);

        sut.RootFolders[0].Children[0].SyncState.ShouldBe(FolderSyncState.Excluded);
    }

    private static (IAuthService Auth, IGraphService Graph) BuildMocks()
    {
        var authService  = Substitute.For<IAuthService>();
        var graphService = Substitute.For<IGraphService>();

        authService.AcquireTokenSilentAsync(AccountIdString, Arg.Any<CancellationToken>())
            .Returns(AuthResultFactory.Success(AccessToken, AccountIdString, AccountProfileFactory.Create("Test User", "test@test.com")));

        graphService.GetDriveIdAsync(Arg.Any<string>(), Arg.Any<Func<CancellationToken, Task<string>>>(), Arg.Any<CancellationToken>())
            .Returns(new Result<DriveId, string>.Ok(new DriveId(DriveIdValue)));

        graphService.GetRootFoldersAsync(Arg.Any<string>(), Arg.Any<Func<CancellationToken, Task<string>>>(), Arg.Any<CancellationToken>())
            .Returns(new Result<List<DriveFolder>, string>.Ok([new DriveFolder(RootFolderId, RootFolderName, Option.None<string>())]));

        return (authService, graphService);
    }

    private static (IAuthService Auth, IGraphService Graph) BuildMocksWithChild()
    {
        var (authService, graphService) = BuildMocks();

        graphService.GetChildFoldersAsync(Arg.Any<Func<CancellationToken, Task<string>>>(), new DriveId(DriveIdValue), RootFolderId, Arg.Any<CancellationToken>())
            .Returns(new Result<List<DriveFolder>, string>.Ok([new DriveFolder(ChildFolderId, ChildFolderName, RootFolderId)]));

        return (authService, graphService);
    }

    private static OneDriveAccount BuildAccount()
        => new()
        {
            Id         = new AccountId(AccountIdString),
            Profile    = AccountProfileFactory.Create("Test User", "test@test.com"),
            SyncConfig = Option.Some(AccountSyncConfigFactory.Create(ConflictPolicy.LastWriteWins, LocalSyncPath.Restore(LocalSyncPathString)))
        };

    private static AccountFilesViewModel BuildSut((IAuthService Auth, IGraphService Graph) mocks, ISyncRuleRepository syncRuleRepo)
        => new(BuildAccount(), mocks.Auth, mocks.Graph, new SyncRuleService(syncRuleRepo, Substitute.For<ILogger<SyncRuleService>>()), Substitute.For<IFileSystem>(), Substitute.For<IFileManagerService>(), Substitute.For<ILogger<AccountFilesViewModel>>(), new FolderTreeNodeViewModelFactory(mocks.Graph, Substitute.For<ILogger<FolderTreeNodeViewModel>>(), Substitute.For<ILocalizationService>()), Substitute.For<ILocalizationService>());
}
