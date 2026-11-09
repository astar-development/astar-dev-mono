using System.Diagnostics.CodeAnalysis;
using AStarDev.CloudSync.Accounts;
using AStarDev.FunctionalParadigm;
using Microsoft.Extensions.Options;
using Microsoft.Identity.Client;

namespace AStarDev.CloudSync.Accounts.OneDrive;

/// <summary>Calls MSAL to sign in through the system browser, read tokens from the cache and manage cached accounts.</summary>
/// <param name="application">The app's one MSAL public client application.</param>
/// <param name="options">The scopes to request.</param>
[ExcludeFromCodeCoverage(Justification = "Thin wrapper over MSAL's interactive system-browser sign-in and its persisted token cache, which cannot run in automated tests.")]
internal sealed class MsalClient(IPublicClientApplication application, IOptions<MicrosoftAccountOptions> options) : IMsalClient
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

        foreach (var account in accounts.Where(account => string.Equals(account.HomeAccountId?.Identifier, key.Value, StringComparison.Ordinal)))
            await application.RemoveAsync(account).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<MicrosoftSignIn> ReauthenticateAsync(ProviderAccountKey key, string loginHint, CancellationToken cancellationToken)
    {
        var accounts = await application.GetAccountsAsync().ConfigureAwait(false);
        var request = accounts.FirstOrNone(account => string.Equals(account.HomeAccountId?.Identifier, key.Value, StringComparison.Ordinal))
            .Match(account => application.AcquireTokenInteractive(options.Value.Scopes).WithAccount(account), () => application.AcquireTokenInteractive(options.Value.Scopes).WithLoginHint(loginHint));
        var result = await request.WithPrompt(Prompt.SelectAccount).WithUseEmbeddedWebView(false).ExecuteAsync(cancellationToken).ConfigureAwait(false);

        return MicrosoftSignInFactory.Create(result.Account?.HomeAccountId?.Identifier, result.Account?.Username, result.ClaimsPrincipal?.FindFirst("name")?.Value, result.AccessToken, result.Scopes);
    }

    /// <inheritdoc />
    public async Task<Option<string>> AcquireTokenSilentAsync(ProviderAccountKey key, CancellationToken cancellationToken)
    {
        var accounts = await application.GetAccountsAsync().ConfigureAwait(false);
        var cached = accounts.FirstOrNone(account => string.Equals(account.HomeAccountId?.Identifier, key.Value, StringComparison.Ordinal));

        return await cached.MatchAsync(
                async account => Option.Some((await application.AcquireTokenSilent(options.Value.Scopes, account).ExecuteAsync(cancellationToken).ConfigureAwait(false)).AccessToken),
                Option.None<string>)
            .ConfigureAwait(false);
    }
}
