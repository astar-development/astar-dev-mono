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

    /// <summary>The token could not be obtained now, for example because the network or the token cache is unavailable.</summary>
    public sealed record Unavailable : AccessTokenOutcome
    {
        internal Unavailable()
        {
        }
    }
}
