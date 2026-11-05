using System.Diagnostics.CodeAnalysis;
using AStarDev.CloudSync.Core.Accounts;
using Microsoft.Extensions.Options;
using Microsoft.Identity.Client;

namespace AStarDev.CloudSync.Infrastructure.OneDrive;

/// <summary>Calls MSAL to sign in through the system browser and manage cached accounts.</summary>
/// <param name="application">The app's one MSAL public client application.</param>
/// <param name="options">The scopes to request.</param>
[ExcludeFromCodeCoverage(Justification = "Thin wrapper over MSAL's interactive system-browser sign-in, which cannot run in automated tests.")]
public sealed class MsalClient(IPublicClientApplication application, IOptions<MicrosoftAccountOptions> options) : IMsalClient
{
    /// <inheritdoc />
    public async Task<IReadOnlyList<string>> GetAccountKeysAsync() => [.. (await application.GetAccountsAsync().ConfigureAwait(false)).Select(account => account.HomeAccountId?.Identifier ?? string.Empty)];

    /// <inheritdoc />
    public async Task<MicrosoftSignIn> SignInAsync(CancellationToken cancellationToken)
    {
        var result = await application.AcquireTokenInteractive(options.Value.Scopes)
            .WithPrompt(Prompt.SelectAccount)
            .WithUseEmbeddedWebView(false)
            .ExecuteAsync(cancellationToken)
            .ConfigureAwait(false);

        return MicrosoftSignInFactory.Create(result.Account?.HomeAccountId?.Identifier, result.Account?.Username, result.ClaimsPrincipal?.FindFirst("name")?.Value, result.AccessToken, result.Scopes);
    }

    /// <inheritdoc />
    public async Task RemoveAccountAsync(ProviderAccountKey key)
    {
        var accounts = await application.GetAccountsAsync().ConfigureAwait(false);

        foreach (var account in accounts.Where(account => string.Equals(account.HomeAccountId?.Identifier, key.Value, StringComparison.OrdinalIgnoreCase)))
            await application.RemoveAsync(account).ConfigureAwait(false);
    }
}
