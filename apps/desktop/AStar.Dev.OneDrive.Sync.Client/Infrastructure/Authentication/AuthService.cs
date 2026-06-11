using AStar.Dev.Functional.Extensions;
using AStar.Dev.OneDrive.Sync.Client.Data.Entities;
using AStar.Dev.OneDrive.Sync.Client.Infrastructure.ApplicationConfiguration;
using AStar.Dev.OneDrive.Sync.Client.Infrastructure.Logging;
using Microsoft.Extensions.Logging;
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
public sealed class AuthService(IPublicClientApplication app, ITokenCacheService cacheService, IOptions<EntraIdConfiguration> entraIdOptions, ILogger<AuthService> logger) : IAuthService
{
    private readonly ILogger<AuthService> logger = logger;
    private readonly ITokenCacheService cacheService = cacheService;
    private bool cacheRegistered;

    /// <inheritdoc />
    public async Task<Result<AuthResult, AuthError>> SignInInteractiveAsync(CancellationToken ct = default)
    {
        await EnsureCacheRegisteredAsync();

        try
        {
            var result = await app
                    .AcquireTokenInteractive(entraIdOptions.Value.Scopes)
                    .WithPrompt(Prompt.SelectAccount)
                    .WithUseEmbeddedWebView(false)
                    .ExecuteAsync(ct);

            return BuildSuccess(result);
        }
        catch (MsalClientException ex) when (ex.ErrorCode is MsalError.AuthenticationCanceledError or "authentication_canceled" or "user_canceled")
        {
            return AuthResultFactory.Cancelled();
        }
        catch (OperationCanceledException)
        {
            return AuthResultFactory.Cancelled();
        }
        catch (MsalException ex)
        {
            OneDriveSyncClientMessages.AuthInteractiveMsalException(logger, ex.ErrorCode, ex);

            return AuthResultFactory.Failure("Authentication failed.");
        }
        catch (Exception ex)
        {
            OneDriveSyncClientMessages.AuthInteractiveUnexpectedException(logger, ex);

            return AuthResultFactory.Failure("Authentication failed.");
        }
    }

    /// <inheritdoc />
    public async Task<Result<AuthResult, AuthError>> AcquireTokenSilentAsync(string accountId, CancellationToken ct = default)
    {
        await EnsureCacheRegisteredAsync();

        try
        {
            var accounts = await app.GetAccountsAsync();
            var account = accounts.FirstOrDefault(a =>
                string.Equals(a.HomeAccountId?.Identifier, accountId, StringComparison.OrdinalIgnoreCase)
                || string.Equals(a.Username, accountId, StringComparison.OrdinalIgnoreCase));

            if (account is null)
                return AuthResultFactory.ReAuthRequired("no_account", "None");

            var result = await app
                .AcquireTokenSilent(entraIdOptions.Value.Scopes, account)
                .ExecuteAsync(ct);

            return BuildSuccess(result);
        }
        catch (MsalUiRequiredException ex)
        {
            OneDriveSyncClientMessages.AuthSilentTokenUiRequired(logger, ex.ErrorCode, ex.Classification.ToString());

            return AuthResultFactory.ReAuthRequired(ex.ErrorCode, ex.Classification.ToString());
        }
        catch (OperationCanceledException)
        {
            return AuthResultFactory.Cancelled();
        }
        catch (MsalException ex)
        {
            OneDriveSyncClientMessages.AuthSilentMsalException(logger, ex.ErrorCode, ex);

            return AuthResultFactory.Failure("Token refresh failed.");
        }
    }

    /// <inheritdoc />
    public async Task SignOutAsync(string accountId, CancellationToken ct = default)
    {
        await EnsureCacheRegisteredAsync();

        var accounts = await app.GetAccountsAsync();
        var account = accounts.FirstOrDefault(a => a.HomeAccountId?.Identifier == accountId);

        if (account is not null)
            await app.RemoveAsync(account);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<string>> GetCachedAccountIdsAsync()
    {
        await EnsureCacheRegisteredAsync();

        var accounts = await app.GetAccountsAsync();

        return accounts
            .Select(account => account.HomeAccountId.Identifier)
            .ToList().AsReadOnly();
    }

    private async Task EnsureCacheRegisteredAsync()
    {
        if (cacheRegistered)
            return;

        await cacheService.RegisterAsync(app);
        cacheRegistered = true;
    }

    private static Result<AuthResult, AuthError> BuildSuccess(AuthenticationResult result)
    {
        string displayName = ClaimsProfileResolver.ResolveDisplayName(result.ClaimsPrincipal, result.Account.Username);
        string email = ClaimsProfileResolver.ResolveEmail(result.ClaimsPrincipal, result.Account.Username);

        return AuthResultFactory.Success(result.AccessToken, result.Account.HomeAccountId.Identifier, AccountProfileFactory.Create(displayName, email), result.ExpiresOn);
    }
}
