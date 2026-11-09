using AStarDev.CloudSync.Accounts;
using AStarDev.FunctionalParadigm;

namespace AStarDev.CloudSync.Accounts.OneDrive;

/// <summary>The MSAL calls the add-account flow and the access token source need.</summary>
public interface IMsalClient
{
    /// <summary>Lists the home account identifiers already in the token cache.</summary>
    Task<IReadOnlyList<string>> GetAccountKeysAsync();

    /// <summary>Signs the user in through the system browser.</summary>
    /// <param name="cancellationToken">Cancels the sign-in.</param>
    Task<MicrosoftSignIn> SignInAsync(CancellationToken cancellationToken);

    /// <summary>Removes the account from the token cache.</summary>
    /// <param name="key">The home account identifier, matched ignoring case.</param>
    Task RemoveAccountAsync(ProviderAccountKey key);

    /// <summary>Gets an access token for the account from the token cache without user interaction, refreshing it when needed.</summary>
    /// <param name="key">The home account identifier, matched ignoring case.</param>
    /// <param name="cancellationToken">Cancels the request.</param>
    /// <returns>The token, or none when the token cache has no such account. Throws MSAL's <c>MsalUiRequiredException</c> when the user must sign in again.</returns>
    Task<Option<string>> AcquireTokenSilentAsync(ProviderAccountKey key, CancellationToken cancellationToken);

    /// <summary>Signs a stored account in again through the system browser, pre-selecting it (the cached account, or <paramref name="loginHint"/> when it is no longer cached).</summary>
    /// <param name="key">The stored account's home account identifier.</param>
    /// <param name="loginHint">The stored account's email, used when the cache no longer holds the account.</param>
    /// <param name="cancellationToken">Cancels the sign-in.</param>
    /// <returns>The sign-in, which may be a different account if the user chose one.</returns>
    Task<MicrosoftSignIn> ReauthenticateAsync(ProviderAccountKey key, string loginHint, CancellationToken cancellationToken);
}
