namespace AStarDev.CloudSync.Accounts.OneDrive;

/// <summary>The result of asking for an account's Microsoft Graph access token without user interaction.</summary>
public abstract record AccessTokenOutcome
{
    private AccessTokenOutcome()
    {
    }

    /// <summary>A token was read from, or refreshed through, the token cache.</summary>
    public sealed record Acquired : AccessTokenOutcome
    {
        internal Acquired(string token) => Token = token;

        /// <summary>The bearer token for Microsoft Graph.</summary>
        public string Token { get; }

        /// <summary>Returns a redacted form, so the token never reaches logs by accident.</summary>
        public override string ToString() => "Acquired(redacted)";
    }

    /// <summary>The user must sign in again before a token can be issued.</summary>
    public sealed record SignInRequired : AccessTokenOutcome
    {
        internal SignInRequired()
        {
        }
    }

    /// <summary>No stored account has the requested id.</summary>
    public sealed record UnknownAccount : AccessTokenOutcome
    {
        internal UnknownAccount()
        {
        }
    }

    /// <summary>The token cache cannot be connected without asking the user, for example because there is no secure store and plaintext storage was never agreed to. The user must give consent through the add-account flow.</summary>
    public sealed record TokenCacheUnavailable : AccessTokenOutcome
    {
        internal TokenCacheUnavailable()
        {
        }
    }

    /// <summary>The token could not be obtained now, for example because the network is unavailable or the request timed out.</summary>
    public sealed record Unavailable : AccessTokenOutcome
    {
        internal Unavailable()
        {
        }
    }
}
