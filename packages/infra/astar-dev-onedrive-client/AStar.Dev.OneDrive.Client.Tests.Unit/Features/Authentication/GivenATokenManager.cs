using AStar.Dev.OneDrive.Client.Features.Authentication;
using AStar.Dev.Functional.Extensions;
using AccessToken = AStar.Dev.OneDrive.Client.Features.Authentication.AccessToken;

namespace AStar.Dev.OneDrive.Client.Tests.Unit.Features.Authentication;

public sealed class GivenATokenManager : IDisposable
{
    private readonly Guid _accountId = Guid.NewGuid();
    private readonly IMsalClient _msalClient = Substitute.For<IMsalClient>();
    private readonly AuthStateService _authStateService = new();
    private TokenManager _tokenManager = null!;

    public void Dispose() => _authStateService.Dispose();

    private void SetUp() => _tokenManager = new TokenManager(_accountId, _msalClient, _authStateService);

    [Fact]
    public async Task when_get_token_silently_async_and_msal_returns_expired_token_then_returns_failure()
    {
        SetUp();
        _msalClient.AcquireTokenSilentAsync(Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<Result<AccessToken, string>>(new Result<AccessToken, string>.Error("Token expired")));

        var result = await _tokenManager.GetTokenSilentlyAsync(TestContext.Current.CancellationToken);

        result.ShouldBeOfType<Result<AccessToken, string>.Error>().Reason.ShouldBe("Token expired");
    }

    [Fact]
    public async Task when_get_token_silently_async_and_msal_returns_valid_token_then_returns_success()
    {
        SetUp();
        var validToken = new AccessToken("valid_token", DateTimeOffset.UtcNow.AddHours(1));
        _msalClient.AcquireTokenSilentAsync(Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<Result<AccessToken, string>>(new Result<AccessToken, string>.Ok(validToken)));

        var result = await _tokenManager.GetTokenSilentlyAsync(TestContext.Current.CancellationToken);

        var token = result.ShouldBeOfType<Result<AccessToken, string>.Ok>().Value;
        token.Token.ShouldBe("valid_token");
        token.IsExpired.ShouldBeFalse();
    }

    [Fact]
    public async Task when_get_token_silently_async_and_msal_fails_then_publishes_auth_required()
    {
        SetUp();
        var stateChanges = new List<(Guid, AccountAuthState)>();
        var subscription = _authStateService.AccountAuthStateChanged.Subscribe(change => stateChanges.Add(change));

        _msalClient.AcquireTokenSilentAsync(Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<Result<AccessToken, string>>(new Result<AccessToken, string>.Error("Auth required")));

        var result = await _tokenManager.GetTokenSilentlyAsync(TestContext.Current.CancellationToken);

        result.ShouldBeOfType<Result<AccessToken, string>.Error>();
        stateChanges.Count.ShouldBe(1);
        stateChanges[0].Item1.ShouldBe(_accountId);
        stateChanges[0].Item2.ShouldBe(AccountAuthState.AuthRequired);

        subscription.Dispose();
    }

    [Fact]
    public async Task when_persist_token_async_then_stores_token_in_memory()
    {
        SetUp();
        var token = new AccessToken("new_token", DateTimeOffset.UtcNow.AddHours(1));

        var persistResult = await _tokenManager.PersistTokenAsync(token, null, TestContext.Current.CancellationToken);
        await _tokenManager.GetTokenSilentlyAsync(TestContext.Current.CancellationToken);

        persistResult.ShouldBeOfType<Result<bool, string>.Ok>();
        _ = _msalClient.Received(0).AcquireTokenSilentAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task when_clear_token_async_then_removes_stored_token()
    {
        SetUp();
        var token = new AccessToken("new_token", DateTimeOffset.UtcNow.AddHours(1));
        _msalClient.AcquireTokenSilentAsync(Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<Result<AccessToken, string>>(new Result<AccessToken, string>.Error("No cached token")));
        await _tokenManager.PersistTokenAsync(token, null, TestContext.Current.CancellationToken);

        var clearResult = await _tokenManager.ClearTokenAsync(TestContext.Current.CancellationToken);
        await _tokenManager.GetTokenSilentlyAsync(TestContext.Current.CancellationToken);

        clearResult.ShouldBeOfType<Result<bool, string>.Ok>();
        _ = await _msalClient.Received(1).AcquireTokenSilentAsync(Arg.Any<CancellationToken>());
    }
}
