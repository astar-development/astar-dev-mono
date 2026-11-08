namespace AStarDev.CloudSync.Accounts.OneDrive;

/// <summary>The account details and access token from one interactive Microsoft sign-in.</summary>
public sealed record MicrosoftSignIn
{
    internal MicrosoftSignIn(string accountKey, string email, string displayName, string accessToken, IReadOnlyList<string> grantedScopes)
    {
        AccountKey = accountKey;
        Email = email;
        DisplayName = displayName;
        AccessToken = accessToken;
        GrantedScopes = grantedScopes;
    }

    /// <summary>The MSAL home account identifier.</summary>
    public string AccountKey { get; }

    /// <summary>The account's sign-in name.</summary>
    public string Email { get; }

    /// <summary>The account holder's name from the ID token.</summary>
    public string DisplayName { get; }

    /// <summary>The access token for Microsoft Graph.</summary>
    public string AccessToken { get; }

    /// <summary>The Microsoft Graph permissions the token was granted.</summary>
    public IReadOnlyList<string> GrantedScopes { get; }

    /// <summary>Hides the account details and token so they never reach a log.</summary>
    public override string ToString() => "MicrosoftSignIn(redacted)";
}
