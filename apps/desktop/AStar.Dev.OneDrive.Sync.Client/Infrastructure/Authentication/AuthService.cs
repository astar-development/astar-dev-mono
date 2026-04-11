using AStar.Dev.OneDrive.Sync.Client.Infrastructure.ApplicationConfiguration;
using Microsoft.Extensions.Options;
using Microsoft.Identity.Client;

namespace AStar.Dev.OneDrive.Sync.Client.Infrastructure.Authentication;

/// <summary>
/// MSAL-backed authentication service for OneDrive personal accounts.
///
/// Uses the system browser + loopback redirect (http://localhost) which
/// works on both Linux and Windows without requiring WebView2.
///
/// Scopes requested:
///   Files.ReadWrite     — read/write files in OneDrive
///   offline_access      — get refresh tokens so the app works without re-auth
///   User.Read           — get display name and email from the profile
/// </summary>
public sealed class AuthService(ITokenCacheService cacheService, IOptions<EntraIdConfiguration> entraIdOptions) : IAuthService
{
    private readonly IPublicClientApplication _app = PublicClientApplicationBuilder
            .Create(entraIdOptions.Value.ClientId)
            .WithAuthority(entraIdOptions.Value.AuthorityForMicrosoftAccountsOnly)
            .WithRedirectUri(entraIdOptions.Value.RedirectUri)
            .WithClientName("AStar.Dev.OneDrive.Sync")
            .WithClientVersion("1.0.0")
            .Build();

    private readonly ITokenCacheService _cacheService = cacheService;
    private          bool                     _cacheRegistered;

    public async Task<AuthResult> SignInInteractiveAsync(CancellationToken ct = default)
    {
        await EnsureCacheRegisteredAsync();

        try
        {
            var result = await _app
                    .AcquireTokenInteractive(entraIdOptions.Value.Scopes)
                    .WithPrompt(Prompt.SelectAccount)
                    .ExecuteAsync(ct);

            return BuildSuccess(result);
        }
        catch(MsalClientException ex) when(ex.ErrorCode is MsalError.AuthenticationCanceledError or "authentication_canceled" or "user_canceled")
        {
            return AuthResult.Cancelled();
        }
        catch(OperationCanceledException)
        {
            return AuthResult.Cancelled();
        }
        catch(MsalException ex)
        {
            return AuthResult.Failure($"Authentication failed: {ex.Message}");
        }
        catch(Exception ex)
        {
            return AuthResult.Failure($"Unexpected error during sign-in: {ex.Message}");
        }
    }

    public async Task<AuthResult> AcquireTokenSilentAsync(string accountId, CancellationToken ct = default)
    {
        await EnsureCacheRegisteredAsync();

        try
        {
            IEnumerable<IAccount> accounts = await _app.GetAccountsAsync();
            var account  = accounts.FirstOrDefault(a => a.HomeAccountId.Identifier == accountId);

            if(account is null)
                return AuthResult.Failure("Account not found in token cache.");

            var result = await _app
                .AcquireTokenSilent(entraIdOptions.Value.Scopes, account)
                .ExecuteAsync(ct);

            return BuildSuccess(result);
        }
        catch(MsalUiRequiredException)
        {
            return AuthResult.Failure("Re-authentication required.");
        }
        catch(OperationCanceledException)
        {
            return AuthResult.Cancelled();
        }
        catch(MsalException ex)
        {
            return AuthResult.Failure($"Token refresh failed: {ex.Message}");
        }
    }

    public async Task SignOutAsync(string accountId, CancellationToken ct = default)
    {
        await EnsureCacheRegisteredAsync();

        IEnumerable<IAccount> accounts = await _app.GetAccountsAsync();
        var account  = accounts.FirstOrDefault(a => a.HomeAccountId.Identifier == accountId);

        if(account is not null)
            await _app.RemoveAsync(account);
    }

    public async Task<IReadOnlyList<string>> GetCachedAccountIdsAsync()
    {
        await EnsureCacheRegisteredAsync();

        IEnumerable<IAccount> accounts = await _app.GetAccountsAsync();
        return accounts
            .Select(a => a.HomeAccountId.Identifier)
            .ToList().AsReadOnly();
    }

    private async Task EnsureCacheRegisteredAsync()
    {
        if(_cacheRegistered)
            return;

        await _cacheService.RegisterAsync(_app);
        _cacheRegistered = true;
    }

    private static AuthResult BuildSuccess(AuthenticationResult result)
    {
        string? displayName = result.Account.Username;
        string? email       = result.Account.Username;

        if(result.ClaimsPrincipal is not null)
        {
            string? nameClaim  = result.ClaimsPrincipal.FindFirst("name")?.Value;
            string? emailClaim = result.ClaimsPrincipal.FindFirst("preferred_username")?.Value
                                 ?? result.ClaimsPrincipal.FindFirst("email")?.Value;

            if(!string.IsNullOrEmpty(nameClaim))
                displayName = nameClaim;
            if(!string.IsNullOrEmpty(emailClaim))
                email = emailClaim;
        }

        return AuthResult.Success(
            accessToken: result.AccessToken,
            accountId: result.Account.HomeAccountId.Identifier,
            displayName: displayName,
            email: email);
    }
}
