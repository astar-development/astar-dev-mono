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

public sealed class GivenAnAccountFilesViewModelWithAuthFailure
{
    private const string AccountIdString = "account-1";
    private readonly IAuthService authService = Substitute.For<IAuthService>();

    private static OneDriveAccount BuildAccount() => new()
    {
        Id = new AccountId(AccountIdString),
        Profile = AccountProfileFactory.Create("Test User", "test@test.com")
    };

    private static AccountFilesViewModel BuildSut(IAuthService authService)
    {
        var fileSystemServices = new FileSystemServices(Substitute.For<IFileSystem>(), Substitute.For<IFileManagerService>());
        var accountFilesViewServices = new AccountFilesViewServices(authService, Substitute.For<ILocalizationService>(), Substitute.For<IGraphService>(), Substitute.For<ISyncRuleService>());

        return new AccountFilesViewModel(BuildAccount(), accountFilesViewServices, fileSystemServices, Substitute.For<ILogger<AccountFilesViewModel>>(), new FolderTreeNodeViewModelFactory(Substitute.For<IGraphService>(), Substitute.For<ILogger<FolderTreeNodeViewModel>>(), Substitute.For<ILocalizationService>()));
    }

    [Fact]
    public async Task when_token_acquisition_fails_with_auth_failed_error_then_load_error_contains_the_failure_message()
    {
        authService.AcquireTokenSilentAsync(AccountIdString, Arg.Any<CancellationToken>())
            .Returns(AuthResultFactory.Failure("Token has expired"));
        var sut = BuildSut(authService);

        await sut.LoadCommand.ExecuteAsync(null);

        sut.LoadError.ShouldBe("Token has expired");
    }

    [Fact]
    public async Task when_token_acquisition_fails_with_auth_failed_error_then_has_load_error_is_true()
    {
        authService.AcquireTokenSilentAsync(AccountIdString, Arg.Any<CancellationToken>())
            .Returns(AuthResultFactory.Failure("Token has expired"));
        var sut = BuildSut(authService);

        await sut.LoadCommand.ExecuteAsync(null);

        sut.HasLoadError.ShouldBeTrue();
    }

    [Fact]
    public async Task when_token_acquisition_fails_with_auth_failed_error_then_root_folders_remain_empty()
    {
        authService.AcquireTokenSilentAsync(AccountIdString, Arg.Any<CancellationToken>())
            .Returns(AuthResultFactory.Failure("Token has expired"));
        var sut = BuildSut(authService);

        await sut.LoadCommand.ExecuteAsync(null);

        sut.RootFolders.ShouldBeEmpty();
    }

    [Fact]
    public async Task when_token_acquisition_is_cancelled_then_load_error_is_authentication_failed_fallback()
    {
        authService.AcquireTokenSilentAsync(AccountIdString, Arg.Any<CancellationToken>())
            .Returns(AuthResultFactory.Cancelled());
        var sut = BuildSut(authService);

        await sut.LoadCommand.ExecuteAsync(null);

        sut.LoadError.ShouldBe("Authentication failed.");
    }

    [Fact]
    public async Task when_token_acquisition_is_cancelled_then_has_load_error_is_true()
    {
        authService.AcquireTokenSilentAsync(AccountIdString, Arg.Any<CancellationToken>())
            .Returns(AuthResultFactory.Cancelled());
        var sut = BuildSut(authService);

        await sut.LoadCommand.ExecuteAsync(null);

        sut.HasLoadError.ShouldBeTrue();
    }

    [Fact]
    public async Task when_token_acquisition_fails_then_is_loading_is_false_after_return()
    {
        authService.AcquireTokenSilentAsync(AccountIdString, Arg.Any<CancellationToken>())
            .Returns(AuthResultFactory.Failure("Error"));
        var sut = BuildSut(authService);

        await sut.LoadCommand.ExecuteAsync(null);

        sut.IsLoading.ShouldBeFalse();
    }

    [Fact]
    public async Task when_token_acquisition_succeeds_then_has_load_error_is_false()
    {
        var graphService = Substitute.For<IGraphService>();
        authService.AcquireTokenSilentAsync(AccountIdString, Arg.Any<CancellationToken>())
            .Returns(AuthResultFactory.Success("token", AccountIdString, AccountProfileFactory.Create("Test User", "test@test.com")));
        graphService.GetDriveIdAsync(Arg.Any<string>(), Arg.Any<Func<CancellationToken, Task<string>>>(), Arg.Any<CancellationToken>()).Returns(new Ok<DriveId, string>(new DriveId("drive-1")));
        graphService.GetRootFoldersAsync(Arg.Any<string>(), Arg.Any<Func<CancellationToken, Task<string>>>(), Arg.Any<CancellationToken>()).Returns(new Ok<List<DriveFolder>, string>([]));
        var syncRuleRepo = Substitute.For<ISyncRuleRepository>();
        syncRuleRepo.GetByAccountIdAsync(Arg.Any<AccountId>(), Arg.Any<CancellationToken>()).Returns([]);
        var fileSystemServices = new FileSystemServices(Substitute.For<IFileSystem>(), Substitute.For<IFileManagerService>());

        var accountFilesViewServices = new AccountFilesViewServices(authService, Substitute.For<ILocalizationService>(), graphService, new SyncRuleService(syncRuleRepo, Substitute.For<ILogger<SyncRuleService>>()));
        var sut = new AccountFilesViewModel(BuildAccount(), accountFilesViewServices, fileSystemServices, Substitute.For<ILogger<AccountFilesViewModel>>(), new FolderTreeNodeViewModelFactory(graphService, Substitute.For<ILogger<FolderTreeNodeViewModel>>(), Substitute.For<ILocalizationService>()));

        await sut.LoadCommand.ExecuteAsync(null);

        sut.HasLoadError.ShouldBeFalse();
    }
}
