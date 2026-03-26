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

    public async Task<MsalAuthResult> SignInInteractiveAsync(CancellationToken ct = default)
    {
        var result = await _pca.AcquireTokenInteractive(Scopes).WithUseEmbeddedWebView(false).ExecuteAsync(ct);
        return new MsalAuthResult(result.Account.HomeAccountId.Identifier, result.Account.Username, result.Account.Username, result.AccessToken);
    }

    public async Task SignOutAsync(string accountId, CancellationToken ct = default)
    {
        var accounts = await _pca.GetAccountsAsync();
        var account = accounts.FirstOrDefault(a => a.HomeAccountId.Identifier == accountId);
        if (account is not null)
        {
            await _pca.RemoveAsync(account);
        }
    }

    public async Task<string> AcquireTokenSilentAsync(string accountId, CancellationToken ct = default)
    {
        var accounts = await _pca.GetAccountsAsync();
        var account = accounts.FirstOrDefault(a => a.HomeAccountId.Identifier == accountId) ?? throw new InvalidOperationException($"Account {accountId} not found in MSAL cache.");
        var result = await _pca.AcquireTokenSilent(Scopes, account).ExecuteAsync(ct);
        return result.AccessToken;
    }
}
