using System.IO.Abstractions;
using AStar.Dev.OneDrive.Sync.Client.Accounts;
using AStar.Dev.OneDrive.Sync.Client.Data.Repositories;
using AStar.Dev.OneDrive.Sync.Client.Domain;
using AStar.Dev.OneDrive.Sync.Client.Infrastructure.Authentication;
using AStar.Dev.OneDrive.Sync.Client.Infrastructure.Graph;

namespace AStar.Dev.OneDrive.Sync.Client.Tests.Unit.Accounts;

public sealed class GivenAnAccountFilesViewModelWithAuthFailure
{
    private const string AccountIdString = "account-1";

    private static OneDriveAccount BuildAccount() => new()
    {
        Id      = new AccountId(AccountIdString),
        Profile = AccountProfileFactory.Create("Test User", "test@test.com")
    };

    private static AccountFilesViewModel BuildSut(IAuthService authService)
    {
        var syncRuleRepo = Substitute.For<ISyncRuleRepository>();

        return new AccountFilesViewModel(BuildAccount(), authService, Substitute.For<IGraphService>(), Substitute.For<IAccountRepository>(), syncRuleRepo, Substitute.For<IFileSystem>());
    }

    [Fact]
    public async Task when_token_acquisition_fails_with_auth_failed_error_then_load_error_contains_the_failure_message()
    {
        var authService = Substitute.For<IAuthService>();
        authService.AcquireTokenSilentAsync(AccountIdString, Arg.Any<CancellationToken>())
            .Returns(AuthResultFactory.Failure("Token has expired"));
        var sut = BuildSut(authService);

        await sut.LoadCommand.ExecuteAsync(null);

        sut.LoadError.ShouldBe("Token has expired");
    }

    [Fact]
    public async Task when_token_acquisition_fails_with_auth_failed_error_then_has_load_error_is_true()
    {
        var authService = Substitute.For<IAuthService>();
        authService.AcquireTokenSilentAsync(AccountIdString, Arg.Any<CancellationToken>())
            .Returns(AuthResultFactory.Failure("Token has expired"));
        var sut = BuildSut(authService);

        await sut.LoadCommand.ExecuteAsync(null);

        sut.HasLoadError.ShouldBeTrue();
    }

    [Fact]
    public async Task when_token_acquisition_fails_with_auth_failed_error_then_root_folders_remain_empty()
    {
        var authService = Substitute.For<IAuthService>();
        authService.AcquireTokenSilentAsync(AccountIdString, Arg.Any<CancellationToken>())
            .Returns(AuthResultFactory.Failure("Token has expired"));
        var sut = BuildSut(authService);

        await sut.LoadCommand.ExecuteAsync(null);

        sut.RootFolders.ShouldBeEmpty();
    }

    [Fact]
    public async Task when_token_acquisition_is_cancelled_then_load_error_is_authentication_failed_fallback()
    {
        var authService = Substitute.For<IAuthService>();
        authService.AcquireTokenSilentAsync(AccountIdString, Arg.Any<CancellationToken>())
            .Returns(AuthResultFactory.Cancelled());
        var sut = BuildSut(authService);

        await sut.LoadCommand.ExecuteAsync(null);

        sut.LoadError.ShouldBe("Authentication failed.");
    }

    [Fact]
    public async Task when_token_acquisition_is_cancelled_then_has_load_error_is_true()
    {
        var authService = Substitute.For<IAuthService>();
        authService.AcquireTokenSilentAsync(AccountIdString, Arg.Any<CancellationToken>())
            .Returns(AuthResultFactory.Cancelled());
        var sut = BuildSut(authService);

        await sut.LoadCommand.ExecuteAsync(null);

        sut.HasLoadError.ShouldBeTrue();
    }

    [Fact]
    public async Task when_token_acquisition_fails_then_is_loading_is_false_after_return()
    {
        var authService = Substitute.For<IAuthService>();
        authService.AcquireTokenSilentAsync(AccountIdString, Arg.Any<CancellationToken>())
            .Returns(AuthResultFactory.Failure("Error"));
        var sut = BuildSut(authService);

        await sut.LoadCommand.ExecuteAsync(null);

        sut.IsLoading.ShouldBeFalse();
    }

    [Fact]
    public async Task when_token_acquisition_succeeds_then_has_load_error_is_false()
    {
        var authService  = Substitute.For<IAuthService>();
        var graphService = Substitute.For<IGraphService>();
        authService.AcquireTokenSilentAsync(AccountIdString, Arg.Any<CancellationToken>())
            .Returns(AuthResultFactory.Success("token", AccountIdString, AccountProfileFactory.Create("Test User", "test@test.com")));
        graphService.GetDriveIdAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns("drive-1");
        graphService.GetRootFoldersAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns([]);
        var syncRuleRepo = Substitute.For<ISyncRuleRepository>();
        syncRuleRepo.GetByAccountIdAsync(Arg.Any<AccountId>(), Arg.Any<CancellationToken>()).Returns([]);

        var sut = new AccountFilesViewModel(BuildAccount(), authService, graphService, Substitute.For<IAccountRepository>(), syncRuleRepo, Substitute.For<IFileSystem>());

        await sut.LoadCommand.ExecuteAsync(null);

        sut.HasLoadError.ShouldBeFalse();
    }
}
