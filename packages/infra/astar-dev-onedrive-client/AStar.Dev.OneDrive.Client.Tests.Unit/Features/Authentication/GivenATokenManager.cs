using AStar.Dev.OneDrive.Client.Features.Authentication;
using AStar.Dev.Functional.Extensions;

namespace AStar.Dev.OneDrive.Client.Tests.Unit.Features.Authentication;

public sealed class GivenATokenManager
{
    private readonly Guid _accountId = Guid.NewGuid();
    private readonly IMsalClient _msalClient = Substitute.For<IMsalClient>();
    private readonly IAuthStateService _authStateService = new AuthStateService();
    private TokenManager _tokenManager = null!;

    private void SetUp() => _tokenManager = new TokenManager(_accountId, _msalClient, _authStateService);

    [Fact]
    public async Task when_get_token_silently_async_and_msal_returns_expired_token_then_returns_failure()
    {
        SetUp();
        var expiredToken = new AccessToken("old_token", DateTimeOffset.UtcNow.AddHours(-1));
        _msalClient.AcquireTokenSilentAsync(CancellationToken.None)
            .Returns(Task.FromResult((Result<AccessToken, string>)new Result<AccessToken, string>.Error("Token expired")));

        var result = await _tokenManager.GetTokenSilentlyAsync();

        result.Match(
            onSuccess: _ => throw new Xunit.Sdk.XunitException("Should have failed"),
            onFailure: error => error.ShouldBe("Token expired"));
    }

    [Fact]
    public async Task when_get_token_silently_async_and_msal_returns_valid_token_then_returns_success()
    {
        SetUp();
        var validToken = new AccessToken("valid_token", DateTimeOffset.UtcNow.AddHours(1));
        _msalClient.AcquireTokenSilentAsync(CancellationToken.None)
            .Returns(Task.FromResult((Result<AccessToken, string>)new Result<AccessToken, string>.Ok(validToken)));

        var result = await _tokenManager.GetTokenSilentlyAsync();

        result.Match(
            onSuccess: token =>
            {
                token.Token.ShouldBe("valid_token");
                token.IsExpired.ShouldBeFalse();
            },
            onFailure: _ => throw new Xunit.Sdk.XunitException("Should have succeeded"));
    }

    [Fact]
    public async Task when_get_token_silently_async_and_msal_fails_then_publishes_auth_required()
    {
        SetUp();
        var stateChanges = new List<(Guid, AccountAuthState)>();
        var subscription = _authStateService.AccountAuthStateChanged.Subscribe(change => stateChanges.Add(change));

        _msalClient.AcquireTokenSilentAsync(CancellationToken.None)
            .Returns(Task.FromResult((Result<AccessToken, string>)new Result<AccessToken, string>.Error("Auth required")));

        var result = await _tokenManager.GetTokenSilentlyAsync();

        result.Match(
            onSuccess: _ => throw new Xunit.Sdk.XunitException("Should have failed"),
            onFailure: _ =>
            {
                stateChanges.Count.ShouldBe(1);
                stateChanges[0].Item1.ShouldBe(_accountId);
                stateChanges[0].Item2.ShouldBe(AccountAuthState.AuthRequired);
            });

        subscription.Dispose();
    }

    [Fact]
    public async Task when_persist_token_async_then_stores_token_in_memory()
    {
        SetUp();
        var token = new AccessToken("new_token", DateTimeOffset.UtcNow.AddHours(1));

        var persistResult = await _tokenManager.PersistTokenAsync(token, null);
        var getResult = await _tokenManager.GetTokenSilentlyAsync();

        persistResult.Match(
            onSuccess: _ => { },
            onFailure: e => throw new Xunit.Sdk.XunitException($"Persist failed: {e}"));

        // Mock should not be called if token is cached and not expiring
        _msalClient.Received(0).AcquireTokenSilentAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task when_clear_token_async_then_removes_stored_token()
    {
        SetUp();
        var token = new AccessToken("new_token", DateTimeOffset.UtcNow.AddHours(1));
        await _tokenManager.PersistTokenAsync(token, null);

        var clearResult = await _tokenManager.ClearTokenAsync();
        var getResult = await _tokenManager.GetTokenSilentlyAsync();

        clearResult.Match(
            onSuccess: _ => { },
            onFailure: e => throw new Xunit.Sdk.XunitException($"Clear failed: {e}"));

        await _msalClient.Received(1).AcquireTokenSilentAsync(Arg.Any<CancellationToken>());
    }
}
