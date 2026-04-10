using AStar.Dev.OneDrive.Sync.Client.Accounts;
using AStar.Dev.OneDrive.Sync.Client.Data.Entities;
using AStar.Dev.OneDrive.Sync.Client.Data.Repositories;
using AStar.Dev.OneDrive.Sync.Client.Infrastructure.Authentication;
using AStar.Dev.OneDrive.Sync.Client.Infrastructure.Graph;
using AStar.Dev.OneDrive.Sync.Client.Models;

namespace AStar.Dev.OneDrive.Sync.Client.Tests.Unit.Accounts;

public sealed class GivenAnAccountFilesViewModelWithAConfiguredSyncPath
{
    private const string AccountId     = "account-1";
    private const string LocalSyncPath = "/configured/sync/path";
    private const string AccessToken   = "token-abc";
    private const string DriveId       = "drive-1";
    private const string FolderId      = "folder-1";
    private const string FolderName    = "Photos";

    [Fact]
    public async Task when_a_folder_is_toggled_then_the_local_sync_path_is_preserved_in_the_repository()
    {
        var authService  = Substitute.For<IAuthService>();
        var graphService = Substitute.For<IGraphService>();
        var repository   = Substitute.For<IAccountRepository>();

        authService.AcquireTokenSilentAsync(AccountId, Arg.Any<CancellationToken>())
            .Returns(AuthResult.Success(AccessToken, AccountId, "Test User", "test@test.com"));

        graphService.GetDriveIdAsync(AccessToken, Arg.Any<CancellationToken>())
            .Returns(DriveId);

        graphService.GetRootFoldersAsync(AccessToken, Arg.Any<CancellationToken>())
            .Returns([new DriveFolder(FolderId, FolderName)]);

        var account = new OneDriveAccount
        {
            Id             = AccountId,
            DisplayName    = "Test User",
            Email          = "test@test.com",
            LocalSyncPath  = LocalSyncPath,
            ConflictPolicy = ConflictPolicy.LastWriteWins
        };

        var sut = new AccountFilesViewModel(account, authService, graphService, repository);

        await sut.LoadCommand.ExecuteAsync(null);

        AccountEntity? savedEntity = null;
        await repository.UpsertAsync(Arg.Do<AccountEntity>(e => savedEntity = e), Arg.Any<CancellationToken>());

        sut.RootFolders[0].ToggleIncludeCommand.Execute(null);

        await repository.Received(1).UpsertAsync(Arg.Any<AccountEntity>(), Arg.Any<CancellationToken>());
        savedEntity.ShouldNotBeNull();
        savedEntity!.LocalSyncPath.ShouldBe(LocalSyncPath);
    }

    [Fact]
    public async Task when_a_folder_is_toggled_then_the_conflict_policy_is_preserved_in_the_repository()
    {
        var authService  = Substitute.For<IAuthService>();
        var graphService = Substitute.For<IGraphService>();
        var repository   = Substitute.For<IAccountRepository>();

        authService.AcquireTokenSilentAsync(AccountId, Arg.Any<CancellationToken>())
            .Returns(AuthResult.Success(AccessToken, AccountId, "Test User", "test@test.com"));

        graphService.GetDriveIdAsync(AccessToken, Arg.Any<CancellationToken>())
            .Returns(DriveId);

        graphService.GetRootFoldersAsync(AccessToken, Arg.Any<CancellationToken>())
            .Returns([new DriveFolder(FolderId, FolderName)]);

        var account = new OneDriveAccount
        {
            Id             = AccountId,
            DisplayName    = "Test User",
            Email          = "test@test.com",
            LocalSyncPath  = LocalSyncPath,
            ConflictPolicy = ConflictPolicy.LastWriteWins
        };

        var sut = new AccountFilesViewModel(account, authService, graphService, repository);

        await sut.LoadCommand.ExecuteAsync(null);

        AccountEntity? savedEntity = null;
        await repository.UpsertAsync(Arg.Do<AccountEntity>(e => savedEntity = e), Arg.Any<CancellationToken>());

        sut.RootFolders[0].ToggleIncludeCommand.Execute(null);

        await repository.Received(1).UpsertAsync(Arg.Any<AccountEntity>(), Arg.Any<CancellationToken>());
        savedEntity.ShouldNotBeNull();
        savedEntity!.ConflictPolicy.ShouldBe(ConflictPolicy.LastWriteWins);
    }
}
