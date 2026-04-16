using AStar.Dev.OneDrive.Sync.Client.Accounts;
using AStar.Dev.OneDrive.Sync.Client.Data.Entities;
using AStar.Dev.OneDrive.Sync.Client.Data.Repositories;
using AStar.Dev.OneDrive.Sync.Client.Domain;
using AStar.Dev.OneDrive.Sync.Client.Infrastructure.Authentication;
using AStar.Dev.OneDrive.Sync.Client.Infrastructure.Graph;
using AStar.Dev.OneDrive.Sync.Client.Models;

namespace AStar.Dev.OneDrive.Sync.Client.Tests.Unit.Accounts;

public sealed class GivenAnAccountFilesViewModelWithAConfiguredSyncPath
{
    private const string AccountIdString  = "account-1";
    private const string LocalSyncPathString = "/configured/sync/path";
    private const string AccessToken      = "token-abc";
    private const string DriveId          = "drive-1";
    private const string FolderId         = "folder-1";
    private const string FolderName       = "Photos";
    private const string ChildFolderId    = "folder-1-child";
    private const string ChildFolderName  = "Holidays";

    [Fact]
    public async Task when_a_folder_is_toggled_then_the_local_sync_path_is_preserved_in_the_repository()
    {
        var (authService, graphService, repository) = BuildMocks();

        repository.GetByIdAsync(new AccountId(AccountIdString), Arg.Any<CancellationToken>())
            .Returns(BuildStoredEntity(LocalSyncPathString, ConflictPolicy.LastWriteWins));

        var sut = BuildSut(BuildAccount(LocalSyncPathString, ConflictPolicy.LastWriteWins), authService, graphService, repository);

        await sut.LoadCommand.ExecuteAsync(null);

        AccountEntity? savedEntity = null;
        await repository.UpsertAsync(Arg.Do<AccountEntity>(e => savedEntity = e), Arg.Any<CancellationToken>());

        sut.RootFolders[0].ToggleIncludeCommand.Execute(null);

        await repository.Received(1).UpsertAsync(Arg.Any<AccountEntity>(), Arg.Any<CancellationToken>());
        savedEntity.ShouldNotBeNull();
        savedEntity!.LocalSyncPath.Value.ShouldBe(LocalSyncPathString);
    }

    [Fact]
    public async Task when_a_folder_is_toggled_then_the_conflict_policy_is_preserved_in_the_repository()
    {
        var (authService, graphService, repository) = BuildMocks();

        repository.GetByIdAsync(new AccountId(AccountIdString), Arg.Any<CancellationToken>())
            .Returns(BuildStoredEntity(LocalSyncPathString, ConflictPolicy.LastWriteWins));

        var sut = BuildSut(BuildAccount(LocalSyncPathString, ConflictPolicy.LastWriteWins), authService, graphService, repository);

        await sut.LoadCommand.ExecuteAsync(null);

        AccountEntity? savedEntity = null;
        await repository.UpsertAsync(Arg.Do<AccountEntity>(e => savedEntity = e), Arg.Any<CancellationToken>());

        sut.RootFolders[0].ToggleIncludeCommand.Execute(null);

        await repository.Received(1).UpsertAsync(Arg.Any<AccountEntity>(), Arg.Any<CancellationToken>());
        savedEntity.ShouldNotBeNull();
        savedEntity!.ConflictPolicy.ShouldBe(ConflictPolicy.LastWriteWins);
    }

    [Fact]
    public async Task when_a_folder_is_toggled_and_the_in_memory_account_has_a_stale_empty_path_then_the_db_local_sync_path_is_preserved()
    {
        var (authService, graphService, repository) = BuildMocks();

        repository.GetByIdAsync(new AccountId(AccountIdString), Arg.Any<CancellationToken>())
            .Returns(BuildStoredEntity(LocalSyncPathString, ConflictPolicy.RemoteWins));

        var staleAccount = BuildAccount(localSyncPath: string.Empty, ConflictPolicy.Ignore);

        var sut = BuildSut(staleAccount, authService, graphService, repository);

        await sut.LoadCommand.ExecuteAsync(null);

        AccountEntity? savedEntity = null;
        await repository.UpsertAsync(Arg.Do<AccountEntity>(e => savedEntity = e), Arg.Any<CancellationToken>());

        sut.RootFolders[0].ToggleIncludeCommand.Execute(null);

        await repository.Received(1).UpsertAsync(Arg.Any<AccountEntity>(), Arg.Any<CancellationToken>());
        savedEntity.ShouldNotBeNull();
        savedEntity!.LocalSyncPath.Value.ShouldBe(LocalSyncPathString);
        savedEntity!.ConflictPolicy.ShouldBe(ConflictPolicy.RemoteWins);
    }

    [Fact]
    public async Task when_a_child_folder_is_toggled_included_then_the_child_folder_is_persisted_to_the_repository()
    {
        var (authService, graphService, repository) = BuildMocksWithChild();

        repository.GetByIdAsync(new AccountId(AccountIdString), Arg.Any<CancellationToken>())
            .Returns(BuildStoredEntity(LocalSyncPathString, ConflictPolicy.Ignore));

        var sut = BuildSut(BuildAccount(LocalSyncPathString, ConflictPolicy.Ignore), authService, graphService, repository);

        await sut.LoadCommand.ExecuteAsync(null);
        await sut.RootFolders[0].ToggleExpandCommand.ExecuteAsync(null);

        AccountEntity? savedEntity = null;
        await repository.UpsertAsync(Arg.Do<AccountEntity>(e => savedEntity = e), Arg.Any<CancellationToken>());

        sut.RootFolders[0].Children[0].ToggleIncludeCommand.Execute(null);

        await repository.Received(1).UpsertAsync(Arg.Any<AccountEntity>(), Arg.Any<CancellationToken>());
        savedEntity.ShouldNotBeNull();
        savedEntity!.SyncFolders.ShouldContain(f => f.FolderId == new OneDriveFolderId(ChildFolderId));
    }

    [Fact]
    public async Task when_a_child_folder_is_toggled_included_and_root_is_also_included_then_both_are_persisted()
    {
        var (authService, graphService, repository) = BuildMocksWithChild();

        repository.GetByIdAsync(new AccountId(AccountIdString), Arg.Any<CancellationToken>())
            .Returns(BuildStoredEntity(LocalSyncPathString, ConflictPolicy.Ignore));

        var sut = BuildSut(BuildAccount(LocalSyncPathString, ConflictPolicy.Ignore), authService, graphService, repository);

        await sut.LoadCommand.ExecuteAsync(null);

        sut.RootFolders[0].ToggleIncludeCommand.Execute(null);

        await sut.RootFolders[0].ToggleExpandCommand.ExecuteAsync(null);

        AccountEntity? savedEntity = null;
        await repository.UpsertAsync(Arg.Do<AccountEntity>(e => savedEntity = e), Arg.Any<CancellationToken>());

        sut.RootFolders[0].Children[0].ToggleIncludeCommand.Execute(null);

        await repository.Received(2).UpsertAsync(Arg.Any<AccountEntity>(), Arg.Any<CancellationToken>());
        savedEntity.ShouldNotBeNull();
        savedEntity!.SyncFolders.ShouldContain(f => f.FolderId == new OneDriveFolderId(FolderId));
        savedEntity!.SyncFolders.ShouldContain(f => f.FolderId == new OneDriveFolderId(ChildFolderId));
    }

    [Fact]
    public async Task when_a_child_folder_is_toggled_excluded_then_the_child_is_removed_from_persisted_sync_folders()
    {
        var (authService, graphService, repository) = BuildMocksWithChild();

        repository.GetByIdAsync(new AccountId(AccountIdString), Arg.Any<CancellationToken>())
            .Returns(BuildStoredEntity(LocalSyncPathString, ConflictPolicy.Ignore));

        var sut = BuildSut(BuildAccount(LocalSyncPathString, ConflictPolicy.Ignore), authService, graphService, repository);

        await sut.LoadCommand.ExecuteAsync(null);
        await sut.RootFolders[0].ToggleExpandCommand.ExecuteAsync(null);

        sut.RootFolders[0].Children[0].ToggleIncludeCommand.Execute(null);

        AccountEntity? savedEntity = null;
        await repository.UpsertAsync(Arg.Do<AccountEntity>(e => savedEntity = e), Arg.Any<CancellationToken>());

        sut.RootFolders[0].Children[0].ToggleIncludeCommand.Execute(null);

        savedEntity.ShouldNotBeNull();
        savedEntity!.SyncFolders.ShouldNotContain(f => f.FolderId == new OneDriveFolderId(ChildFolderId));
    }

    private static (IAuthService Auth, IGraphService Graph, IAccountRepository Repository) BuildMocks()
    {
        var authService  = Substitute.For<IAuthService>();
        var graphService = Substitute.For<IGraphService>();
        var repository   = Substitute.For<IAccountRepository>();

        authService.AcquireTokenSilentAsync(AccountIdString, Arg.Any<CancellationToken>())
            .Returns(AuthResult.Success(AccessToken, AccountIdString, "Test User", "test@test.com"));

        graphService.GetDriveIdAsync(AccessToken, Arg.Any<CancellationToken>())
            .Returns(DriveId);

        graphService.GetRootFoldersAsync(AccessToken, Arg.Any<CancellationToken>())
            .Returns([new DriveFolder(FolderId, FolderName)]);

        return (authService, graphService, repository);
    }

    private static (IAuthService Auth, IGraphService Graph, IAccountRepository Repository) BuildMocksWithChild()
    {
        var (authService, graphService, repository) = BuildMocks();

        graphService.GetChildFoldersAsync(AccessToken, DriveId, FolderId, Arg.Any<CancellationToken>())
            .Returns([new DriveFolder(ChildFolderId, ChildFolderName, FolderId)]);

        return (authService, graphService, repository);
    }

    private static OneDriveAccount BuildAccount(string localSyncPath, ConflictPolicy conflictPolicy)
        => new()
        {
            Id             = new AccountId(AccountIdString),
            DisplayName    = "Test User",
            Email          = "test@test.com",
            LocalSyncPath  = string.IsNullOrEmpty(localSyncPath) ? null : LocalSyncPath.Restore(localSyncPath),
            ConflictPolicy = conflictPolicy
        };

    private static AccountEntity BuildStoredEntity(string localSyncPath, ConflictPolicy conflictPolicy)
        => new()
        {
            Id             = new AccountId(AccountIdString),
            DisplayName    = "Test User",
            Email          = "test@test.com",
            LocalSyncPath  = LocalSyncPath.Restore(localSyncPath),
            ConflictPolicy = conflictPolicy
        };

    private static AccountFilesViewModel BuildSut(OneDriveAccount account, IAuthService authService, IGraphService graphService, IAccountRepository repository)
        => new(account, authService, graphService, repository);
}
