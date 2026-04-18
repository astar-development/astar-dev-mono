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
    private const string AccountIdString     = "account-1";
    private const string LocalSyncPathString = "/configured/sync/path";
    private const string AccessToken         = "token-abc";
    private const string FolderId            = "folder-1";
    private const string FolderName          = "Photos";
    private const string ChildFolderId       = "folder-1-child";
    private const string ChildFolderName     = "Holidays";

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

        await repository.Received(2).UpsertAsync(Arg.Any<AccountEntity>(), Arg.Any<CancellationToken>());
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

        await repository.Received(2).UpsertAsync(Arg.Any<AccountEntity>(), Arg.Any<CancellationToken>());
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

        await repository.Received(2).UpsertAsync(Arg.Any<AccountEntity>(), Arg.Any<CancellationToken>());
        savedEntity.ShouldNotBeNull();
        savedEntity!.LocalSyncPath.Value.ShouldBe(LocalSyncPathString);
        savedEntity!.ConflictPolicy.ShouldBe(ConflictPolicy.RemoteWins);
    }

    [Fact]
    public async Task when_parent_is_included_then_parent_and_all_children_are_persisted()
    {
        var (authService, graphService, repository) = BuildMocksWithChild();

        repository.GetByIdAsync(new AccountId(AccountIdString), Arg.Any<CancellationToken>())
            .Returns(BuildStoredEntity(LocalSyncPathString, ConflictPolicy.Ignore));

        var sut = BuildSut(BuildAccount(LocalSyncPathString, ConflictPolicy.Ignore), authService, graphService, repository);

        await sut.LoadCommand.ExecuteAsync(null);

        AccountEntity? savedEntity = null;
        await repository.UpsertAsync(Arg.Do<AccountEntity>(e => savedEntity = e), Arg.Any<CancellationToken>());

        sut.RootFolders[0].ToggleIncludeCommand.Execute(null);

        await repository.Received(2).UpsertAsync(Arg.Any<AccountEntity>(), Arg.Any<CancellationToken>());
        savedEntity.ShouldNotBeNull();
        savedEntity!.SyncFolders.ShouldContain(f => f.FolderId == new OneDriveFolderId(FolderId) && !f.IsExplicitlyExcluded && f.FolderName == FolderName);
        savedEntity!.SyncFolders.ShouldContain(f => f.FolderId == new OneDriveFolderId(ChildFolderId) && !f.IsExplicitlyExcluded);
    }

    [Fact]
    public async Task when_root_is_included_and_child_is_explicitly_excluded_then_both_are_persisted_with_correct_flags()
    {
        var (authService, graphService, repository) = BuildMocksWithChild();

        repository.GetByIdAsync(new AccountId(AccountIdString), Arg.Any<CancellationToken>())
            .Returns(BuildStoredEntity(LocalSyncPathString, ConflictPolicy.Ignore));

        var sut = BuildSut(BuildAccount(LocalSyncPathString, ConflictPolicy.Ignore), authService, graphService, repository);

        await sut.LoadCommand.ExecuteAsync(null);

        sut.RootFolders[0].ToggleIncludeCommand.Execute(null);

        AccountEntity? savedEntity = null;
        await repository.UpsertAsync(Arg.Do<AccountEntity>(e => savedEntity = e), Arg.Any<CancellationToken>());

        sut.RootFolders[0].Children[0].ToggleIncludeCommand.Execute(null);

        await repository.Received(3).UpsertAsync(Arg.Any<AccountEntity>(), Arg.Any<CancellationToken>());
        savedEntity.ShouldNotBeNull();
        savedEntity!.SyncFolders.ShouldContain(f => f.FolderId == new OneDriveFolderId(FolderId) && !f.IsExplicitlyExcluded);
        savedEntity!.SyncFolders.ShouldContain(f => f.FolderId == new OneDriveFolderId(ChildFolderId) && f.IsExplicitlyExcluded);
    }

    [Fact]
    public async Task when_a_child_folder_is_toggled_excluded_then_the_child_is_stored_as_not_included()
    {
        var (authService, graphService, repository) = BuildMocksWithChild();

        repository.GetByIdAsync(new AccountId(AccountIdString), Arg.Any<CancellationToken>())
            .Returns(BuildStoredEntity(LocalSyncPathString, ConflictPolicy.Ignore));

        var sut = BuildSut(BuildAccount(LocalSyncPathString, ConflictPolicy.Ignore), authService, graphService, repository);

        await sut.LoadCommand.ExecuteAsync(null);

        sut.RootFolders[0].Children[0].ToggleIncludeCommand.Execute(null);

        AccountEntity? savedEntity = null;
        await repository.UpsertAsync(Arg.Do<AccountEntity>(e => savedEntity = e), Arg.Any<CancellationToken>());

        sut.RootFolders[0].Children[0].ToggleIncludeCommand.Execute(null);

        savedEntity.ShouldNotBeNull();
        savedEntity!.SyncFolders.ShouldContain(f => f.FolderId == new OneDriveFolderId(ChildFolderId) && !f.IsIncluded && !f.IsExplicitlyExcluded);
    }

    [Fact]
    public async Task when_included_root_is_toggled_then_included_folders_are_in_sync_folders_without_explicit_exclusion_flag()
    {
        var (authService, graphService, repository) = BuildMocks();

        repository.GetByIdAsync(new AccountId(AccountIdString), Arg.Any<CancellationToken>())
            .Returns(BuildStoredEntity(LocalSyncPathString, ConflictPolicy.Ignore));

        var sut = BuildSut(BuildAccount(LocalSyncPathString, ConflictPolicy.Ignore), authService, graphService, repository);

        await sut.LoadCommand.ExecuteAsync(null);

        AccountEntity? savedEntity = null;
        await repository.UpsertAsync(Arg.Do<AccountEntity>(e => savedEntity = e), Arg.Any<CancellationToken>());

        sut.RootFolders[0].ToggleIncludeCommand.Execute(null);

        savedEntity.ShouldNotBeNull();
        savedEntity!.SyncFolders.ShouldContain(f => f.FolderId == new OneDriveFolderId(FolderId) && !f.IsExplicitlyExcluded);
    }

    [Fact]
    public async Task when_included_root_is_toggled_then_the_folder_name_stored_is_the_folder_name_itself()
    {
        var (authService, graphService, repository) = BuildMocks();

        repository.GetByIdAsync(new AccountId(AccountIdString), Arg.Any<CancellationToken>())
            .Returns(BuildStoredEntity(LocalSyncPathString, ConflictPolicy.Ignore));

        var sut = BuildSut(BuildAccount(LocalSyncPathString, ConflictPolicy.Ignore), authService, graphService, repository);

        await sut.LoadCommand.ExecuteAsync(null);

        AccountEntity? savedEntity = null;
        await repository.UpsertAsync(Arg.Do<AccountEntity>(e => savedEntity = e), Arg.Any<CancellationToken>());

        sut.RootFolders[0].ToggleIncludeCommand.Execute(null);

        savedEntity.ShouldNotBeNull();
        savedEntity!.SyncFolders.ShouldContain(f => f.FolderId == new OneDriveFolderId(FolderId) && f.FolderName == FolderName);
    }

    [Fact]
    public async Task when_explicitly_excluded_child_has_full_relative_path_as_folder_name()
    {
        var (authService, graphService, repository) = BuildMocksWithChild();

        repository.GetByIdAsync(new AccountId(AccountIdString), Arg.Any<CancellationToken>())
            .Returns(BuildStoredEntity(LocalSyncPathString, ConflictPolicy.Ignore));

        var sut = BuildSut(BuildAccount(LocalSyncPathString, ConflictPolicy.Ignore), authService, graphService, repository);

        await sut.LoadCommand.ExecuteAsync(null);

        sut.RootFolders[0].ToggleIncludeCommand.Execute(null);

        AccountEntity? savedEntity = null;
        await repository.UpsertAsync(Arg.Do<AccountEntity>(e => savedEntity = e), Arg.Any<CancellationToken>());

        sut.RootFolders[0].Children[0].ToggleIncludeCommand.Execute(null);

        savedEntity.ShouldNotBeNull();
        savedEntity!.SyncFolders.ShouldContain(f => f.FolderId == new OneDriveFolderId(ChildFolderId) && f.FolderName == $"{FolderName}/{ChildFolderName}");
    }

    [Fact]
    public async Task when_previously_included_root_is_toggled_to_excluded_then_all_folders_are_stored_as_not_included()
    {
        var (authService, graphService, repository) = BuildMocksWithChild();

        repository.GetByIdAsync(new AccountId(AccountIdString), Arg.Any<CancellationToken>())
            .Returns(BuildStoredEntity(LocalSyncPathString, ConflictPolicy.Ignore));

        var accountWithFolderSelected = new OneDriveAccount
        {
            Id                   = new AccountId(AccountIdString),
            DisplayName          = "Test User",
            Email                = "test@test.com",
            LocalSyncPath        = LocalSyncPath.Restore(LocalSyncPathString),
            ConflictPolicy       = ConflictPolicy.Ignore,
            SelectedFolderIds    = [new OneDriveFolderId(FolderId)],
            AllIncludedFolderIds = [new OneDriveFolderId(FolderId), new OneDriveFolderId(ChildFolderId)]
        };

        var sut = BuildSut(accountWithFolderSelected, authService, graphService, repository);

        await sut.LoadCommand.ExecuteAsync(null);

        AccountEntity? savedEntity = null;
        await repository.UpsertAsync(Arg.Do<AccountEntity>(e => savedEntity = e), Arg.Any<CancellationToken>());

        sut.RootFolders[0].ToggleIncludeCommand.Execute(null);

        savedEntity.ShouldNotBeNull();
        savedEntity!.SyncFolders.ShouldAllBe(f => !f.IsIncluded && !f.IsExplicitlyExcluded);
    }

    [Fact]
    public async Task when_load_completes_then_child_folders_are_pre_populated_without_expanding()
    {
        var (authService, graphService, repository) = BuildMocksWithChild();
        var sut = BuildSut(BuildAccount(LocalSyncPathString, ConflictPolicy.Ignore), authService, graphService, repository);

        await sut.LoadCommand.ExecuteAsync(null);

        sut.RootFolders[0].Children.ShouldHaveSingleItem();
        sut.RootFolders[0].Children[0].Id.ShouldBe(ChildFolderId);
    }

    [Fact]
    public async Task when_load_completes_then_root_folder_has_children_flag_set()
    {
        var (authService, graphService, repository) = BuildMocksWithChild();
        var sut = BuildSut(BuildAccount(LocalSyncPathString, ConflictPolicy.Ignore), authService, graphService, repository);

        await sut.LoadCommand.ExecuteAsync(null);

        sut.RootFolders[0].HasChildren.ShouldBeTrue();
    }

    [Fact]
    public async Task when_load_completes_and_no_children_then_has_children_is_false()
    {
        var (authService, graphService, repository) = BuildMocks();
        var sut = BuildSut(BuildAccount(LocalSyncPathString, ConflictPolicy.Ignore), authService, graphService, repository);

        await sut.LoadCommand.ExecuteAsync(null);

        sut.RootFolders[0].HasChildren.ShouldBeFalse();
    }

    [Fact]
    public async Task when_root_in_all_included_folder_ids_then_child_loads_as_included()
    {
        var (authService, graphService, repository) = BuildMocksWithChild();

        var accountWithBothIncluded = new OneDriveAccount
        {
            Id                   = new AccountId(AccountIdString),
            DisplayName          = "Test User",
            Email                = "test@test.com",
            LocalSyncPath        = LocalSyncPath.Restore(LocalSyncPathString),
            ConflictPolicy       = ConflictPolicy.Ignore,
            SelectedFolderIds    = [new OneDriveFolderId(FolderId)],
            AllIncludedFolderIds = [new OneDriveFolderId(FolderId), new OneDriveFolderId(ChildFolderId)]
        };

        var sut = BuildSut(accountWithBothIncluded, authService, graphService, repository);

        await sut.LoadCommand.ExecuteAsync(null);

        sut.RootFolders[0].Children[0].SyncState.ShouldBe(FolderSyncState.Included);
    }

    [Fact]
    public async Task when_child_not_in_db_but_parent_is_included_then_child_inherits_included_state()
    {
        var (authService, graphService, repository) = BuildMocksWithChild();

        var accountWithRootOnly = new OneDriveAccount
        {
            Id                   = new AccountId(AccountIdString),
            DisplayName          = "Test User",
            Email                = "test@test.com",
            LocalSyncPath        = LocalSyncPath.Restore(LocalSyncPathString),
            ConflictPolicy       = ConflictPolicy.Ignore,
            SelectedFolderIds    = [new OneDriveFolderId(FolderId)],
            AllIncludedFolderIds = [new OneDriveFolderId(FolderId)]
        };

        var sut = BuildSut(accountWithRootOnly, authService, graphService, repository);

        await sut.LoadCommand.ExecuteAsync(null);

        sut.RootFolders[0].Children[0].SyncState.ShouldBe(FolderSyncState.Included);
    }

    [Fact]
    public async Task when_child_is_in_all_included_but_explicitly_excluded_then_excluded_takes_priority()
    {
        var (authService, graphService, repository) = BuildMocksWithChild();

        var accountWithExplicitExclusion = new OneDriveAccount
        {
            Id                          = new AccountId(AccountIdString),
            DisplayName                 = "Test User",
            Email                       = "test@test.com",
            LocalSyncPath               = LocalSyncPath.Restore(LocalSyncPathString),
            ConflictPolicy              = ConflictPolicy.Ignore,
            SelectedFolderIds           = [new OneDriveFolderId(FolderId)],
            AllIncludedFolderIds        = [new OneDriveFolderId(FolderId)],
            ExplicitlyExcludedFolderIds = [new OneDriveFolderId(ChildFolderId)]
        };

        var sut = BuildSut(accountWithExplicitExclusion, authService, graphService, repository);

        await sut.LoadCommand.ExecuteAsync(null);

        sut.RootFolders[0].Children[0].SyncState.ShouldBe(FolderSyncState.Excluded);
    }

    [Fact]
    public async Task when_parent_is_included_then_child_is_also_stored_as_not_explicitly_excluded()
    {
        var (authService, graphService, repository) = BuildMocksWithChild();

        repository.GetByIdAsync(new AccountId(AccountIdString), Arg.Any<CancellationToken>())
            .Returns(BuildStoredEntity(LocalSyncPathString, ConflictPolicy.Ignore));

        var sut = BuildSut(BuildAccount(LocalSyncPathString, ConflictPolicy.Ignore), authService, graphService, repository);

        await sut.LoadCommand.ExecuteAsync(null);

        AccountEntity? savedEntity = null;
        await repository.UpsertAsync(Arg.Do<AccountEntity>(e => savedEntity = e), Arg.Any<CancellationToken>());

        sut.RootFolders[0].ToggleIncludeCommand.Execute(null);

        savedEntity.ShouldNotBeNull();
        savedEntity!.SyncFolders.ShouldContain(f => f.FolderId == new OneDriveFolderId(ChildFolderId) && !f.IsExplicitlyExcluded);
    }

    [Fact]
    public async Task when_root_folder_is_in_selected_folder_ids_only_then_it_loads_as_included()
    {
        var (authService, graphService, repository) = BuildMocksWithChild();

        var accountWithWizardSelection = new OneDriveAccount
        {
            Id                   = new AccountId(AccountIdString),
            DisplayName          = "Test User",
            Email                = "test@test.com",
            LocalSyncPath        = LocalSyncPath.Restore(LocalSyncPathString),
            ConflictPolicy       = ConflictPolicy.Ignore,
            SelectedFolderIds    = [new OneDriveFolderId(FolderId)],
            AllIncludedFolderIds = []
        };

        var sut = BuildSut(accountWithWizardSelection, authService, graphService, repository);

        await sut.LoadCommand.ExecuteAsync(null);

        sut.RootFolders[0].SyncState.ShouldBe(FolderSyncState.Included);
    }

    [Fact]
    public async Task when_root_folder_is_in_selected_folder_ids_only_then_child_inherits_included_state()
    {
        var (authService, graphService, repository) = BuildMocksWithChild();

        var accountWithWizardSelection = new OneDriveAccount
        {
            Id                   = new AccountId(AccountIdString),
            DisplayName          = "Test User",
            Email                = "test@test.com",
            LocalSyncPath        = LocalSyncPath.Restore(LocalSyncPathString),
            ConflictPolicy       = ConflictPolicy.Ignore,
            SelectedFolderIds    = [new OneDriveFolderId(FolderId)],
            AllIncludedFolderIds = []
        };

        var sut = BuildSut(accountWithWizardSelection, authService, graphService, repository);

        await sut.LoadCommand.ExecuteAsync(null);

        sut.RootFolders[0].Children[0].SyncState.ShouldBe(FolderSyncState.Included);
    }

    [Fact]
    public async Task when_db_has_folders_then_onedrive_is_not_called()
    {
        var (authService, graphService, repository) = BuildMocksWithChild();

        var storedEntityWithRootOnly = BuildStoredEntity(LocalSyncPathString, ConflictPolicy.Ignore);
        storedEntityWithRootOnly.SyncFolders = [new SyncFolderEntity
        {
            FolderId   = new OneDriveFolderId(FolderId),
            FolderName = FolderName,
            AccountId  = new AccountId(AccountIdString),
            IsIncluded = true
        }];

        repository.GetByIdAsync(new AccountId(AccountIdString), Arg.Any<CancellationToken>())
            .Returns(storedEntityWithRootOnly);

        var sut = BuildSut(BuildAccount(LocalSyncPathString, ConflictPolicy.Ignore), authService, graphService, repository);

        await sut.LoadCommand.ExecuteAsync(null);

        await graphService.DidNotReceive().GetAllFoldersAsync(Arg.Any<string>(), Arg.Any<CancellationToken>());
        await graphService.DidNotReceive().GetRootFoldersAsync(Arg.Any<string>(), Arg.Any<CancellationToken>());
        sut.RootFolders.ShouldHaveSingleItem();
        sut.RootFolders[0].Id.ShouldBe(FolderId);
    }

    [Fact]
    public async Task when_load_finds_db_matches_current_tree_then_no_extra_upsert_is_called()
    {
        var (authService, graphService, repository) = BuildMocksWithChild();

        var storedEntityWithBothFolders = BuildStoredEntity(LocalSyncPathString, ConflictPolicy.Ignore);
        storedEntityWithBothFolders.SyncFolders =
        [
            new SyncFolderEntity { FolderId = new OneDriveFolderId(FolderId),      FolderName = FolderName,                          AccountId = new AccountId(AccountIdString), IsIncluded = true },
            new SyncFolderEntity { FolderId = new OneDriveFolderId(ChildFolderId), FolderName = $"{FolderName}/{ChildFolderName}", AccountId = new AccountId(AccountIdString), IsIncluded = true }
        ];

        repository.GetByIdAsync(new AccountId(AccountIdString), Arg.Any<CancellationToken>())
            .Returns(storedEntityWithBothFolders);

        var accountWithBothIncluded = new OneDriveAccount
        {
            Id                   = new AccountId(AccountIdString),
            DisplayName          = "Test User",
            Email                = "test@test.com",
            LocalSyncPath        = LocalSyncPath.Restore(LocalSyncPathString),
            ConflictPolicy       = ConflictPolicy.Ignore,
            SelectedFolderIds    = [new OneDriveFolderId(FolderId)],
            AllIncludedFolderIds = [new OneDriveFolderId(FolderId), new OneDriveFolderId(ChildFolderId)]
        };

        var sut = BuildSut(accountWithBothIncluded, authService, graphService, repository);

        await sut.LoadCommand.ExecuteAsync(null);

        await repository.DidNotReceive().UpsertAsync(Arg.Any<AccountEntity>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task when_load_completes_with_all_folders_excluded_then_all_api_folders_are_still_persisted_to_db()
    {
        var (authService, graphService, repository) = BuildMocksWithChild();

        var storedEntityEmpty = BuildStoredEntity(LocalSyncPathString, ConflictPolicy.Ignore);
        repository.GetByIdAsync(new AccountId(AccountIdString), Arg.Any<CancellationToken>())
            .Returns(storedEntityEmpty);

        var accountWithNothingSelected = BuildAccount(LocalSyncPathString, ConflictPolicy.Ignore);
        var sut = BuildSut(accountWithNothingSelected, authService, graphService, repository);

        AccountEntity? savedEntity = null;
        await repository.UpsertAsync(Arg.Do<AccountEntity>(e => savedEntity = e), Arg.Any<CancellationToken>());

        await sut.LoadCommand.ExecuteAsync(null);

        await repository.Received(1).UpsertAsync(Arg.Any<AccountEntity>(), Arg.Any<CancellationToken>());
        savedEntity.ShouldNotBeNull();
        savedEntity!.SyncFolders.ShouldContain(f => f.FolderId == new OneDriveFolderId(FolderId)      && !f.IsIncluded && !f.IsExplicitlyExcluded);
        savedEntity!.SyncFolders.ShouldContain(f => f.FolderId == new OneDriveFolderId(ChildFolderId) && !f.IsIncluded && !f.IsExplicitlyExcluded);
    }

    private static (IAuthService Auth, IGraphService Graph, IAccountRepository Repository) BuildMocks()
    {
        var authService  = Substitute.For<IAuthService>();
        var graphService = Substitute.For<IGraphService>();
        var repository   = Substitute.For<IAccountRepository>();

        authService.AcquireTokenSilentAsync(AccountIdString, Arg.Any<CancellationToken>())
            .Returns(AuthResult.Success(AccessToken, AccountIdString, "Test User", "test@test.com"));

        graphService.GetRootFoldersAsync(AccessToken, Arg.Any<CancellationToken>())
            .Returns([new DriveFolder(FolderId, FolderName)]);

        graphService.GetAllFoldersAsync(AccessToken, Arg.Any<CancellationToken>())
            .Returns([]);

        return (authService, graphService, repository);
    }

    private static (IAuthService Auth, IGraphService Graph, IAccountRepository Repository) BuildMocksWithChild()
    {
        var (authService, graphService, repository) = BuildMocks();

        graphService.GetAllFoldersAsync(AccessToken, Arg.Any<CancellationToken>())
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
