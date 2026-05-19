using System.IO.Abstractions;
using AStar.Dev.Functional.Extensions;
using AStar.Dev.OneDrive.Sync.Client.Accounts;
using AStar.Dev.OneDrive.Sync.Client.Data.Entities;
using AStar.Dev.OneDrive.Sync.Client.Data.Repositories;
using AStar.Dev.OneDrive.Sync.Client.Domain;
using AStar.Dev.OneDrive.Sync.Client.Home;
using AStar.Dev.OneDrive.Sync.Client.Infrastructure.Authentication;
using AStar.Dev.OneDrive.Sync.Client.Infrastructure.Graph;
using AStar.Dev.OneDrive.Sync.Client.Infrastructure.Shell;
using AccountId = AStar.Dev.OneDrive.Sync.Client.Data.Entities.AccountId;

namespace AStar.Dev.OneDrive.Sync.Client.Tests.Unit.Accounts;

public sealed class GivenAnAccountFilesViewModelWithAConfiguredSyncPath
{
    private const string AccountIdString  = "account-1";
    private const string LocalSyncPathString = "/configured/sync/path";
    private const string AccessToken      = "token-abc";
    private const string DriveIdValue      = "drive-1";
    private const string FolderId         = "folder-1";
    private const string FolderName       = "Photos";
    private const string ChildFolderId    = "folder-1-child";
    private const string ChildFolderName  = "Holidays";

    [Fact]
    public async Task when_a_folder_is_toggled_then_repository_upsert_is_not_called()
    {
        var (authService, graphService, repository) = BuildMocks();
        var sut = BuildSut(BuildAccount(LocalSyncPathString, ConflictPolicy.LastWriteWins), authService, graphService, repository);

        await sut.LoadCommand.ExecuteAsync(null);
        sut.RootFolders[0].ToggleIncludeCommand.Execute(null);

        await repository.DidNotReceive().UpsertAsync(Arg.Any<AccountEntity>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task when_a_folder_is_toggled_then_sync_rule_is_persisted()
    {
        var (authService, graphService, repository) = BuildMocks();
        var syncRuleRepo = Substitute.For<ISyncRuleRepository>();
        syncRuleRepo.GetByAccountIdAsync(Arg.Any<AccountId>(), Arg.Any<CancellationToken>())
            .Returns([]);

        var sut = new AccountFilesViewModel(BuildAccount(LocalSyncPathString, ConflictPolicy.LastWriteWins), authService, graphService, repository, syncRuleRepo, Substitute.For<IFileSystem>(), Substitute.For<IFileManagerService>());

        await sut.LoadCommand.ExecuteAsync(null);
        sut.RootFolders[0].ToggleIncludeCommand.Execute(null);

        await syncRuleRepo.Received().UpsertAsync(Arg.Any<AccountId>(), Arg.Any<string>(), Arg.Any<RuleType>(), Arg.Any<string?>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task when_a_parent_folder_is_toggled_included_then_sync_rules_are_written_for_loaded_children()
    {
        var (authService, graphService, repository) = BuildMocksWithChild();

        repository.GetByIdAsync(new AccountId(AccountIdString), Arg.Any<CancellationToken>())
            .Returns(Option.Some(BuildStoredEntity(LocalSyncPathString, ConflictPolicy.Ignore)));

        var syncRuleRepo = Substitute.For<ISyncRuleRepository>();
        syncRuleRepo.GetByAccountIdAsync(Arg.Any<AccountId>(), Arg.Any<CancellationToken>())
            .Returns([]);

        var sut = new AccountFilesViewModel(BuildAccount(LocalSyncPathString, ConflictPolicy.Ignore), authService, graphService, repository, syncRuleRepo, Substitute.For<IFileSystem>(), Substitute.For<IFileManagerService>());

        await sut.LoadCommand.ExecuteAsync(null);
        await sut.RootFolders[0].ToggleExpandCommand.ExecuteAsync(null);

        sut.RootFolders[0].ToggleIncludeCommand.Execute(null);

        await syncRuleRepo.Received(1).UpsertAsync(Arg.Any<AccountId>(), $"/{FolderName}", RuleType.Include, Arg.Any<string?>(), Arg.Any<CancellationToken>());
        await syncRuleRepo.Received(1).UpsertAsync(Arg.Any<AccountId>(), $"/{FolderName}/{ChildFolderName}", RuleType.Include, Arg.Any<string?>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task when_a_folder_is_toggled_included_then_the_remote_item_id_is_stored_in_the_rule()
    {
        var (authService, graphService, repository) = BuildMocksWithChild();

        repository.GetByIdAsync(new AccountId(AccountIdString), Arg.Any<CancellationToken>())
            .Returns(Option.Some(BuildStoredEntity(LocalSyncPathString, ConflictPolicy.Ignore)));

        var syncRuleRepo = Substitute.For<ISyncRuleRepository>();
        syncRuleRepo.GetByAccountIdAsync(Arg.Any<AccountId>(), Arg.Any<CancellationToken>())
            .Returns([]);

        var sut = new AccountFilesViewModel(BuildAccount(LocalSyncPathString, ConflictPolicy.Ignore), authService, graphService, repository, syncRuleRepo, Substitute.For<IFileSystem>(), Substitute.For<IFileManagerService>());

        await sut.LoadCommand.ExecuteAsync(null);
        await sut.RootFolders[0].ToggleExpandCommand.ExecuteAsync(null);

        sut.RootFolders[0].ToggleIncludeCommand.Execute(null);

        await syncRuleRepo.Received(1).UpsertAsync(Arg.Any<AccountId>(), $"/{FolderName}", RuleType.Include, FolderId, Arg.Any<CancellationToken>());
        await syncRuleRepo.Received(1).UpsertAsync(Arg.Any<AccountId>(), $"/{FolderName}/{ChildFolderName}", RuleType.Include, ChildFolderId, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task when_a_parent_folder_is_toggled_excluded_then_only_the_parent_exclude_rule_is_written()
    {
        var (authService, graphService, repository) = BuildMocksWithChild();

        repository.GetByIdAsync(new AccountId(AccountIdString), Arg.Any<CancellationToken>())
            .Returns(Option.Some(BuildStoredEntity(LocalSyncPathString, ConflictPolicy.Ignore)));

        var syncRuleRepo = Substitute.For<ISyncRuleRepository>();
        syncRuleRepo.GetByAccountIdAsync(Arg.Any<AccountId>(), Arg.Any<CancellationToken>())
            .Returns([new SyncRuleEntity { AccountId = new AccountId(AccountIdString), RemotePath = $"/{FolderName}", RuleType = RuleType.Include }]);

        var sut = new AccountFilesViewModel(BuildAccount(LocalSyncPathString, ConflictPolicy.Ignore), authService, graphService, repository, syncRuleRepo, Substitute.For<IFileSystem>(), Substitute.For<IFileManagerService>());

        await sut.LoadCommand.ExecuteAsync(null);
        await sut.RootFolders[0].ToggleExpandCommand.ExecuteAsync(null);

        sut.RootFolders[0].ToggleIncludeCommand.Execute(null);

        await syncRuleRepo.Received(1).UpsertAsync(Arg.Any<AccountId>(), $"/{FolderName}", RuleType.Exclude, Arg.Any<string?>(), Arg.Any<CancellationToken>());
        await syncRuleRepo.DidNotReceive().UpsertAsync(Arg.Any<AccountId>(), $"/{FolderName}/{ChildFolderName}", RuleType.Exclude, Arg.Any<string?>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task when_a_parent_folder_is_toggled_excluded_then_child_rules_are_deleted_before_upserting()
    {
        var (authService, graphService, repository) = BuildMocksWithChild();

        repository.GetByIdAsync(new AccountId(AccountIdString), Arg.Any<CancellationToken>())
            .Returns(Option.Some(BuildStoredEntity(LocalSyncPathString, ConflictPolicy.Ignore)));

        var syncRuleRepo = Substitute.For<ISyncRuleRepository>();
        syncRuleRepo.GetByAccountIdAsync(Arg.Any<AccountId>(), Arg.Any<CancellationToken>())
            .Returns([new SyncRuleEntity { AccountId = new AccountId(AccountIdString), RemotePath = $"/{FolderName}", RuleType = RuleType.Include }]);

        var sut = new AccountFilesViewModel(BuildAccount(LocalSyncPathString, ConflictPolicy.Ignore), authService, graphService, repository, syncRuleRepo, Substitute.For<IFileSystem>(), Substitute.For<IFileManagerService>());

        await sut.LoadCommand.ExecuteAsync(null);
        await sut.RootFolders[0].ToggleExpandCommand.ExecuteAsync(null);

        sut.RootFolders[0].ToggleIncludeCommand.Execute(null);

        await syncRuleRepo.Received(1).DeleteChildRulesAsync(Arg.Any<AccountId>(), $"/{FolderName}", Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task when_a_parent_folder_is_toggled_excluded_then_only_parent_rule_is_upserted_not_child_rules()
    {
        var (authService, graphService, repository) = BuildMocksWithChild();

        repository.GetByIdAsync(new AccountId(AccountIdString), Arg.Any<CancellationToken>())
            .Returns(Option.Some(BuildStoredEntity(LocalSyncPathString, ConflictPolicy.Ignore)));

        var syncRuleRepo = Substitute.For<ISyncRuleRepository>();
        syncRuleRepo.GetByAccountIdAsync(Arg.Any<AccountId>(), Arg.Any<CancellationToken>())
            .Returns([new SyncRuleEntity { AccountId = new AccountId(AccountIdString), RemotePath = $"/{FolderName}", RuleType = RuleType.Include }]);

        var sut = new AccountFilesViewModel(BuildAccount(LocalSyncPathString, ConflictPolicy.Ignore), authService, graphService, repository, syncRuleRepo, Substitute.For<IFileSystem>(), Substitute.For<IFileManagerService>());

        await sut.LoadCommand.ExecuteAsync(null);
        await sut.RootFolders[0].ToggleExpandCommand.ExecuteAsync(null);

        sut.RootFolders[0].ToggleIncludeCommand.Execute(null);

        await syncRuleRepo.Received(1).UpsertAsync(Arg.Any<AccountId>(), Arg.Any<string>(), RuleType.Exclude, Arg.Any<string?>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task when_a_parent_folder_is_toggled_included_then_child_rules_are_deleted_before_upserting()
    {
        var (authService, graphService, repository) = BuildMocksWithChild();

        repository.GetByIdAsync(new AccountId(AccountIdString), Arg.Any<CancellationToken>())
            .Returns(Option.Some(BuildStoredEntity(LocalSyncPathString, ConflictPolicy.Ignore)));

        var syncRuleRepo = Substitute.For<ISyncRuleRepository>();
        syncRuleRepo.GetByAccountIdAsync(Arg.Any<AccountId>(), Arg.Any<CancellationToken>())
            .Returns([]);

        var sut = new AccountFilesViewModel(BuildAccount(LocalSyncPathString, ConflictPolicy.Ignore), authService, graphService, repository, syncRuleRepo, Substitute.For<IFileSystem>(), Substitute.For<IFileManagerService>());

        await sut.LoadCommand.ExecuteAsync(null);
        await sut.RootFolders[0].ToggleExpandCommand.ExecuteAsync(null);

        sut.RootFolders[0].ToggleIncludeCommand.Execute(null);

        await syncRuleRepo.Received(1).DeleteChildRulesAsync(Arg.Any<AccountId>(), $"/{FolderName}", Arg.Any<CancellationToken>());
    }

    private static (IAuthService Auth, IGraphService Graph, IAccountRepository Repository) BuildMocks()
    {
        var authService  = Substitute.For<IAuthService>();
        var graphService = Substitute.For<IGraphService>();
        var repository   = Substitute.For<IAccountRepository>();

        authService.AcquireTokenSilentAsync(AccountIdString, Arg.Any<CancellationToken>())
            .Returns(AuthResultFactory.Success(AccessToken, AccountIdString, AccountProfileFactory.Create("Test User", "test@test.com")));

        graphService.GetDriveIdAsync(Arg.Any<string>(), AccessToken, Arg.Any<CancellationToken>())
            .Returns(new Result<DriveId, string>.Ok(new DriveId(DriveIdValue)));

        graphService.GetRootFoldersAsync(Arg.Any<string>(), AccessToken, Arg.Any<CancellationToken>())
            .Returns(new Result<List<DriveFolder>, string>.Ok([new DriveFolder(FolderId, FolderName)]));

        return (authService, graphService, repository);
    }

    private static (IAuthService Auth, IGraphService Graph, IAccountRepository Repository) BuildMocksWithChild()
    {
        var (authService, graphService, repository) = BuildMocks();

        graphService.GetChildFoldersAsync(AccessToken, new DriveId(DriveIdValue), FolderId, Arg.Any<CancellationToken>())
            .Returns(new Result<List<DriveFolder>, string>.Ok([new DriveFolder(ChildFolderId, ChildFolderName, FolderId)]));

        return (authService, graphService, repository);
    }

    private static OneDriveAccount BuildAccount(string localSyncPath, ConflictPolicy conflictPolicy)
        => new()
        {
            Id         = new AccountId(AccountIdString),
            Profile    = AccountProfileFactory.Create("Test User", "test@test.com"),
            SyncConfig = string.IsNullOrEmpty(localSyncPath)
                ? null
                : AccountSyncConfigFactory.Create(conflictPolicy, LocalSyncPath.Restore(localSyncPath))
        };

    private static AccountEntity BuildStoredEntity(string localSyncPath, ConflictPolicy conflictPolicy)
        => new()
        {
            Id         = new AccountId(AccountIdString),
            Profile    = AccountProfileFactory.Create("Test User", "test@test.com"),
            SyncConfig = AccountSyncConfigFactory.Create(conflictPolicy, LocalSyncPath.Restore(localSyncPath))
        };

    private static AccountFilesViewModel BuildSut(OneDriveAccount account, IAuthService authService, IGraphService graphService, IAccountRepository repository)
    {
        var syncRuleRepo = Substitute.For<ISyncRuleRepository>();
        syncRuleRepo.GetByAccountIdAsync(Arg.Any<AccountId>(), Arg.Any<CancellationToken>())
            .Returns([]);

        return new(account, authService, graphService, repository, syncRuleRepo, Substitute.For<IFileSystem>(), Substitute.For<IFileManagerService>());
    }
}
