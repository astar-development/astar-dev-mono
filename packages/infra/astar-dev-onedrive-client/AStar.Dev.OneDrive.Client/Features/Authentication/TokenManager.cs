using AStar.Dev.Functional.Extensions;

namespace AStar.Dev.OneDrive.Client.Features.Authentication;

/// <summary>
///     Token manager implementation (AU-02, AU-04, AU-05).
///     Scoped per account. Handles silent refresh and token persistence.
/// </summary>
internal sealed class TokenManager(Guid accountId, IMsalClient msalClient, IAuthStateService authStateService) : ITokenManager
{
    private readonly IMsalClient _msalClient = msalClient ?? throw new ArgumentNullException(nameof(msalClient));
    private readonly IAuthStateService _authStateService = authStateService ?? throw new ArgumentNullException(nameof(authStateService));
    private AccessToken? _currentToken;

    public Guid AccountId { get; } = accountId;

    public async Task<Result<AccessToken, string>> GetTokenSilentlyAsync(CancellationToken ct = default)
    {
        if (_currentToken is not null && !_currentToken.IsExpiringSoon)
            return new Result<AccessToken, string>.Ok(_currentToken);

        var result = await _msalClient.AcquireTokenSilentAsync(ct).ConfigureAwait(false);

        return result.Match(
            onSuccess: token =>
            {
                _currentToken = token;
                return new Result<AccessToken, string>.Ok(token);
            },
            onFailure: error =>
            {
                _authStateService.PublishAuthStateChange(AccountId, AccountAuthState.AuthRequired);
                return (Result<AccessToken, string>)new Result<AccessToken, string>.Error(error);
            });
    }

    public async Task<Result<bool, string>> PersistTokenAsync(AccessToken token, string? refreshToken, CancellationToken ct = default)
    {
        _currentToken = token ?? throw new ArgumentNullException(nameof(token));
        await Task.CompletedTask.ConfigureAwait(false);
        return new Result<bool, string>.Ok(true);
    }

    public async Task<Result<bool, string>> ClearTokenAsync(CancellationToken ct = default)
    {
        _currentToken = null;
        await Task.CompletedTask.ConfigureAwait(false);
        return new Result<bool, string>.Ok(true);
    }
}
