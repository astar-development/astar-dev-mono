namespace AStarDev.CloudSync.Accounts.OneDrive;

/// <summary>The account details and access token from one interactive Microsoft sign-in.</summary>
/// <param name="AccountKey">The MSAL home account identifier.</param>
/// <param name="Email">The account's sign-in name.</param>
/// <param name="DisplayName">The account holder's name from the ID token.</param>
/// <param name="AccessToken">The access token for Microsoft Graph.</param>
/// <param name="GrantedScopes">The Microsoft Graph permissions the token was granted.</param>
public sealed record MicrosoftSignIn(string AccountKey, string Email, string DisplayName, string AccessToken, IReadOnlyList<string> GrantedScopes)
{
    /// <summary>Hides the account details and token so they never reach a log.</summary>
    public override string ToString() => "MicrosoftSignIn(redacted)";
}
