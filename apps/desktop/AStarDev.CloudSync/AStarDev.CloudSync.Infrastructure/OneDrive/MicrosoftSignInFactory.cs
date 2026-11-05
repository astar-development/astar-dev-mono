namespace AStarDev.CloudSync.Infrastructure.OneDrive;

/// <summary>Creates <see cref="MicrosoftSignIn"/> results.</summary>
public static class MicrosoftSignInFactory
{
    /// <summary>Creates a sign-in result. Missing values become empty strings.</summary>
    /// <param name="accountKey">The MSAL home account identifier.</param>
    /// <param name="email">The account's sign-in name.</param>
    /// <param name="displayName">The account holder's name from the ID token.</param>
    /// <param name="accessToken">The access token for Microsoft Graph.</param>
    /// <param name="grantedScopes">The Microsoft Graph permissions the token was granted.</param>
    public static MicrosoftSignIn Create(string? accountKey, string? email, string? displayName, string? accessToken, IEnumerable<string>? grantedScopes) => new(accountKey ?? string.Empty, email ?? string.Empty, displayName ?? string.Empty, accessToken ?? string.Empty, [.. grantedScopes ?? []]);
}
