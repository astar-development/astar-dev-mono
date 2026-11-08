using AStarDev.CloudSync.Accounts;

namespace AStarDev.CloudSync.Accounts.OneDrive;

/// <summary>The MSAL calls the add-account flow needs.</summary>
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
}
