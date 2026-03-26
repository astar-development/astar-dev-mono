using AStar.Dev.Functional.Extensions;
using Microsoft.Identity.Client;

namespace AStar.Dev.OneDriveSync.Services;

/// <summary>AM-01: MSAL interactive OAuth using the system browser.</summary>
public sealed class MsalAuthService : IMsalAuthService
{
    private static readonly string[] Scopes = ["Files.Read", "Files.Read.All", "User.Read", "offline_access"];

    private readonly IPublicClientApplication _pca;

    public MsalAuthService(string clientId, string? redirectUri = null)
    {
        var builder = PublicClientApplicationBuilder.Create(clientId).WithAuthority(AadAuthorityAudience.PersonalMicrosoftAccount).WithDefaultRedirectUri();

        if (!string.IsNullOrEmpty(redirectUri))
        {
            builder = builder.WithRedirectUri(redirectUri);
        }

        _pca = builder.Build();
    }

    public async Task<Result<MsalAuthResult, string>> SignInInteractiveAsync(CancellationToken ct = default)
    {
        try
        {
            var result = await _pca.AcquireTokenInteractive(Scopes).WithUseEmbeddedWebView(false).ExecuteAsync(ct);
            var authResult = new MsalAuthResult(result.Account.HomeAccountId.Identifier, result.Account.Username, result.Account.Username, result.AccessToken);
            return new Result<MsalAuthResult, string>.Ok(authResult);
        }
        catch (OperationCanceledException)
        {
            return new Result<MsalAuthResult, string>.Error("Sign-in was cancelled.");
        }
        catch (MsalException ex)
        {
            return new Result<MsalAuthResult, string>.Error(ex.Message);
        }
    }

    public async Task SignOutAsync(string accountId, CancellationToken ct = default)
    {
        var accounts = await _pca.GetAccountsAsync();
        accounts.FirstOrNone(a => a.HomeAccountId.Identifier == accountId)
            .Tap(account => _pca.RemoveAsync(account));
    }

    public async Task<Result<string, string>> AcquireTokenSilentAsync(string accountId, CancellationToken ct = default)
    {
        var accounts = await _pca.GetAccountsAsync();

        return await accounts
            .FirstOrNone(a => a.HomeAccountId.Identifier == accountId)
            .ToResult(() => $"Account {accountId} not found in MSAL cache.")
            .MatchAsync<Result<string, string>>(
                async account =>
                {
                    var result = await _pca.AcquireTokenSilent(Scopes, account).ExecuteAsync(ct);
                    return new Result<string, string>.Ok(result.AccessToken);
                },
                error => Task.FromResult<Result<string, string>>(new Result<string, string>.Error(error)));
    }
}
