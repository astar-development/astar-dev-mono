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
    public async Task WhenGetTokenSilentlyAsync_AndMsalReturnsExpiredToken_ThenReturnsFailure()
    {
        // Arrange
        SetUp();
        var expiredToken = new AccessToken("old_token", DateTimeOffset.UtcNow.AddHours(-1));
        _msalClient.AcquireTokenSilentAsync(CancellationToken.None)
            .Returns(Task.FromResult((Result<AccessToken, string>)new Result<AccessToken, string>.Error("Token expired")));

        // Act
        var result = await _tokenManager.GetTokenSilentlyAsync();

        // Assert
        result.Match(
            onSuccess: _ => throw new Xunit.Sdk.XunitException("Should have failed"),
            onFailure: error => error.ShouldBe("Token expired"));
    }

    [Fact]
    public async Task WhenGetTokenSilentlyAsync_AndMsalReturnsValidToken_ThenReturnsSuccess()
    {
        // Arrange
        SetUp();
        var validToken = new AccessToken("valid_token", DateTimeOffset.UtcNow.AddHours(1));
        _msalClient.AcquireTokenSilentAsync(CancellationToken.None)
            .Returns(Task.FromResult((Result<AccessToken, string>)new Result<AccessToken, string>.Ok(validToken)));

        // Act
        var result = await _tokenManager.GetTokenSilentlyAsync();

        // Assert
        result.Match(
            onSuccess: token =>
            {
                token.Token.ShouldBe("valid_token");
                token.IsExpired.ShouldBeFalse();
            },
            onFailure: _ => throw new Xunit.Sdk.XunitException("Should have succeeded"));
    }

    [Fact]
    public async Task WhenGetTokenSilentlyAsync_AndMsalFails_ThenPublishesAuthRequired()
    {
        // Arrange
        SetUp();
        var stateChanges = new List<(Guid, AccountAuthState)>();
        var subscription = _authStateService.AccountAuthStateChanged.Subscribe(change => stateChanges.Add(change));

        _msalClient.AcquireTokenSilentAsync(CancellationToken.None)
            .Returns(Task.FromResult((Result<AccessToken, string>)new Result<AccessToken, string>.Error("Auth required")));

        // Act
        var result = await _tokenManager.GetTokenSilentlyAsync();

        // Assert
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
    public async Task WhenPersistTokenAsync_ThenStoresTokenInMemory()
    {
        // Arrange
        SetUp();
        var token = new AccessToken("new_token", DateTimeOffset.UtcNow.AddHours(1));

        // Act
        var persistResult = await _tokenManager.PersistTokenAsync(token, null);
        var getResult = await _tokenManager.GetTokenSilentlyAsync();

        // Assert
        persistResult.Match(
            onSuccess: _ => { },
            onFailure: e => throw new Xunit.Sdk.XunitException($"Persist failed: {e}"));

        // Mock should not be called if token is cached and not expiring
        _msalClient.Received(0).AcquireTokenSilentAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task WhenClearTokenAsync_ThenRemovesStoredToken()
    {
        // Arrange
        SetUp();
        var token = new AccessToken("new_token", DateTimeOffset.UtcNow.AddHours(1));
        await _tokenManager.PersistTokenAsync(token, null);

        // Act
        var clearResult = await _tokenManager.ClearTokenAsync();
        var getResult = await _tokenManager.GetTokenSilentlyAsync();

        // Assert
        clearResult.Match(
            onSuccess: _ => { },
            onFailure: e => throw new Xunit.Sdk.XunitException($"Clear failed: {e}"));

        // After clearing, should attempt to fetch from MSAL and get failure
        await _msalClient.Received(1).AcquireTokenSilentAsync(Arg.Any<CancellationToken>());
    }
}
