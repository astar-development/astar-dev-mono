using AStar.Dev.OneDrive.Sync.Client.Infrastructure.Authentication;

namespace AStar.Dev.OneDrive.Sync.Client.Tests.Unit.Infrastructure.Authentication;

public sealed class GivenACachedTokenFactory
{
    private const string AccountId = "user-1";
    private const string InitialToken = "initial-token";
    private const string RefreshedToken = "refreshed-token";

    private readonly IAuthService _authService = Substitute.For<IAuthService>();

    private CachedTokenFactory CreateSut(DateTimeOffset expiresOn)
        => new(AccountId, _authService, InitialToken, expiresOn);

    private void SetupRefreshSuccess()
        => _authService.AcquireTokenSilentAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(AuthResultFactory.Success(RefreshedToken, AccountId, AccountProfileFactory.Create("User", "user@outlook.com"), DateTimeOffset.UtcNow.AddHours(1)));

    private void SetupRefreshFailure()
        => _authService.AcquireTokenSilentAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(AuthResultFactory.Failure("Token refresh failed"));

    [Fact]
    public async Task when_token_is_not_near_expiry_then_cached_token_is_returned()
    {
        var sut = CreateSut(DateTimeOffset.UtcNow.AddMinutes(10));

        string token = await sut.GetTokenAsync(TestContext.Current.CancellationToken);

        token.ShouldBe(InitialToken);
    }

    [Fact]
    public async Task when_token_is_not_near_expiry_then_auth_service_is_not_called()
    {
        var sut = CreateSut(DateTimeOffset.UtcNow.AddMinutes(10));

        await sut.GetTokenAsync(TestContext.Current.CancellationToken);

        await _authService.DidNotReceive().AcquireTokenSilentAsync(Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task when_token_is_not_near_expiry_and_invoked_multiple_times_then_auth_service_is_not_called()
    {
        var sut = CreateSut(DateTimeOffset.UtcNow.AddMinutes(10));

        await sut.GetTokenAsync(TestContext.Current.CancellationToken);
        await sut.GetTokenAsync(TestContext.Current.CancellationToken);
        await sut.GetTokenAsync(TestContext.Current.CancellationToken);

        await _authService.DidNotReceive().AcquireTokenSilentAsync(Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task when_token_is_near_expiry_then_auth_service_is_called_to_refresh()
    {
        SetupRefreshSuccess();
        var sut = CreateSut(DateTimeOffset.UtcNow.AddMinutes(2));

        await sut.GetTokenAsync(TestContext.Current.CancellationToken);

        await _authService.Received(1).AcquireTokenSilentAsync(AccountId, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task when_token_is_near_expiry_and_refresh_succeeds_then_new_token_is_returned()
    {
        SetupRefreshSuccess();
        var sut = CreateSut(DateTimeOffset.UtcNow.AddMinutes(2));

        string token = await sut.GetTokenAsync(TestContext.Current.CancellationToken);

        token.ShouldBe(RefreshedToken);
    }

    [Fact]
    public async Task when_token_is_near_expiry_and_refresh_fails_then_stale_token_is_returned()
    {
        SetupRefreshFailure();
        var sut = CreateSut(DateTimeOffset.UtcNow.AddMinutes(2));

        string token = await sut.GetTokenAsync(TestContext.Current.CancellationToken);

        token.ShouldBe(InitialToken);
    }

    [Fact]
    public async Task when_token_is_near_expiry_and_refresh_fails_then_no_exception_is_thrown()
    {
        SetupRefreshFailure();
        var sut = CreateSut(DateTimeOffset.UtcNow.AddMinutes(2));

        await Should.NotThrowAsync(() => sut.GetTokenAsync(TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task when_token_is_already_expired_then_auth_service_is_called_to_refresh()
    {
        SetupRefreshSuccess();
        var sut = CreateSut(DateTimeOffset.UtcNow.AddHours(-1));

        await sut.GetTokenAsync(TestContext.Current.CancellationToken);

        await _authService.Received(1).AcquireTokenSilentAsync(AccountId, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task when_token_refreshes_successfully_then_subsequent_calls_do_not_refresh_again()
    {
        SetupRefreshSuccess();
        var sut = CreateSut(DateTimeOffset.UtcNow.AddMinutes(2));

        await sut.GetTokenAsync(TestContext.Current.CancellationToken);
        await sut.GetTokenAsync(TestContext.Current.CancellationToken);

        await _authService.Received(1).AcquireTokenSilentAsync(Arg.Any<string>(), Arg.Any<CancellationToken>());
    }
}
