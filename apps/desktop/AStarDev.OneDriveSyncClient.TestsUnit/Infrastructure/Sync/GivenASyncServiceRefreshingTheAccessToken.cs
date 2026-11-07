using AStarDev.FunctionalParadigm;
using AStarDev.OneDriveSyncClient.Accounts;
using AStarDev.OneDriveSyncClient.Data.Repositories;
using AStarDev.OneDriveSyncClient.Infrastructure.Authentication;
using AStarDev.OneDriveSyncClient.Infrastructure.Sync;
using AStarDev.OneDriveSyncClient.Infrastructure.Sync.Jobs;
using AStarDev.OneDriveSyncClient.Infrastructure.Sync.Pipeline;
using AStarDev.Localization;
using Microsoft.Extensions.Logging;
using AccountId = AStar.Dev.Infrastructure.AppDb.Entities.AccountId;

namespace AStarDev.OneDriveSyncClient.TestsUnit.Infrastructure.Sync;

public sealed class GivenASyncServiceRefreshingTheAccessToken
{
    private const string AccountIdValue = "user-1";
    private const string InitialToken = "initial-token";
    private const string RefreshedToken = "refreshed-token";

    private readonly IAuthService authService = Substitute.For<IAuthService>();
    private readonly ISyncPassOrchestrator syncPassOrchestrator = Substitute.For<ISyncPassOrchestrator>();
    private readonly ILocalizationService localizationService = Substitute.For<ILocalizationService>();
    private readonly List<string> tokensSeenDuringThePass = [];
    private Exception? exceptionDuringThePass;

    public GivenASyncServiceRefreshingTheAccessToken()
    {
        localizationService.GetLocal(Arg.Any<string>()).Returns(x => x.ArgAt<string>(0));
        localizationService.GetLocal(Arg.Any<string>(), Arg.Any<object[]>()).Returns(x => x.ArgAt<string>(0));
    }

    private static Result<AuthResult, AuthError> Success(string token, DateTimeOffset expiresOn) => AuthResultFactory.Success(token, AccountIdValue, AccountProfileFactory.Create("User", "user@outlook.com"), expiresOn);

    private static OneDriveAccount CreateAccount() => new()
    {
        Id = new AccountId(AccountIdValue),
        Profile = AccountProfileFactory.Create(string.Empty, "user@outlook.com"),
        SyncConfig = AccountSyncConfigFactory.Create(ConflictPolicy.Ignore, LocalSyncPath.Restore("/path/to/sync")),
        SelectedFolderIds = []
    };

    private void SetupInitialToken(DateTimeOffset expiresOn)
        => authService.AcquireTokenSilentAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(Success(InitialToken, expiresOn));

    private void SetupInitialTokenThenRefreshSuccess(DateTimeOffset expiresOn)
        => authService.AcquireTokenSilentAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(Success(InitialToken, expiresOn), Success(RefreshedToken, DateTimeOffset.UtcNow.AddHours(1)));

    private void SetupInitialTokenThenRefreshFailure(DateTimeOffset expiresOn)
        => authService.AcquireTokenSilentAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(Success(InitialToken, expiresOn), AuthResultFactory.Failure("Token refresh failed"));

    private async Task SyncInvokingTheTokenFactoryAsync(int invocations)
    {
        syncPassOrchestrator.OrchestrateAsync(Arg.Any<OneDriveAccount>(), Arg.Any<AccountSyncConfig>(), Arg.Any<Func<CancellationToken, Task<string>>>(), Arg.Any<Func<SyncConflict, Task>>(), Arg.Any<Action<SyncProgressEventArgs>>(), Arg.Any<Func<JobCompletedEventArgs, Task>>(), Arg.Any<CancellationToken>())
            .Returns(async callInfo =>
            {
                var tokenFactory = callInfo.ArgAt<Func<CancellationToken, Task<string>>>(2);
                exceptionDuringThePass = await Record.ExceptionAsync(async () =>
                {
                    for (int invocation = 0; invocation < invocations; invocation++)
                        tokensSeenDuringThePass.Add(await tokenFactory(TestContext.Current.CancellationToken));
                });

                return SyncPassResultFactory.Create(true, 0);
            });

        var sut = new SyncService(authService, Substitute.For<ISyncRepository>(), syncPassOrchestrator, Substitute.For<IConflictApplier>(), Substitute.For<ILogger<SyncService>>(), localizationService, System.TimeProvider.System);

        await sut.SyncAccountAsync(CreateAccount(), TestContext.Current.CancellationToken);
    }

    [Fact]
    public async Task when_the_token_is_not_near_expiry_then_the_initial_token_is_returned()
    {
        SetupInitialToken(DateTimeOffset.UtcNow.AddMinutes(10));

        await SyncInvokingTheTokenFactoryAsync(1);

        tokensSeenDuringThePass.ShouldBe([InitialToken]);
    }

    [Fact]
    public async Task when_the_token_is_not_near_expiry_then_the_auth_service_is_not_called_again()
    {
        SetupInitialToken(DateTimeOffset.UtcNow.AddMinutes(10));

        await SyncInvokingTheTokenFactoryAsync(1);

        await authService.Received(1).AcquireTokenSilentAsync(Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task when_the_token_is_not_near_expiry_and_requested_three_times_then_the_auth_service_is_not_called_again()
    {
        SetupInitialToken(DateTimeOffset.UtcNow.AddMinutes(10));

        await SyncInvokingTheTokenFactoryAsync(3);

        await authService.Received(1).AcquireTokenSilentAsync(Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task when_the_token_is_near_expiry_then_the_auth_service_is_called_to_refresh_it()
    {
        SetupInitialTokenThenRefreshSuccess(DateTimeOffset.UtcNow.AddMinutes(2));

        await SyncInvokingTheTokenFactoryAsync(1);

        await authService.Received(2).AcquireTokenSilentAsync(AccountIdValue, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task when_the_token_is_near_expiry_and_the_refresh_succeeds_then_the_refreshed_token_is_returned()
    {
        SetupInitialTokenThenRefreshSuccess(DateTimeOffset.UtcNow.AddMinutes(2));

        await SyncInvokingTheTokenFactoryAsync(1);

        tokensSeenDuringThePass.ShouldBe([RefreshedToken]);
    }

    [Fact]
    public async Task when_the_token_is_near_expiry_and_the_refresh_fails_then_the_stale_token_is_returned()
    {
        SetupInitialTokenThenRefreshFailure(DateTimeOffset.UtcNow.AddMinutes(2));

        await SyncInvokingTheTokenFactoryAsync(1);

        tokensSeenDuringThePass.ShouldBe([InitialToken]);
    }

    [Fact]
    public async Task when_the_token_is_near_expiry_and_the_refresh_fails_then_no_exception_is_thrown()
    {
        SetupInitialTokenThenRefreshFailure(DateTimeOffset.UtcNow.AddMinutes(2));

        await SyncInvokingTheTokenFactoryAsync(1);

        exceptionDuringThePass.ShouldBeNull();
    }

    [Fact]
    public async Task when_the_token_has_already_expired_then_the_auth_service_is_called_to_refresh_it()
    {
        SetupInitialTokenThenRefreshSuccess(DateTimeOffset.UtcNow.AddMinutes(-1));

        await SyncInvokingTheTokenFactoryAsync(1);

        await authService.Received(2).AcquireTokenSilentAsync(AccountIdValue, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task when_the_token_refreshes_successfully_then_later_requests_do_not_refresh_it_again()
    {
        SetupInitialTokenThenRefreshSuccess(DateTimeOffset.UtcNow.AddMinutes(2));

        await SyncInvokingTheTokenFactoryAsync(3);

        tokensSeenDuringThePass.ShouldBe([RefreshedToken, RefreshedToken, RefreshedToken]);
        await authService.Received(2).AcquireTokenSilentAsync(AccountIdValue, Arg.Any<CancellationToken>());
    }
}
