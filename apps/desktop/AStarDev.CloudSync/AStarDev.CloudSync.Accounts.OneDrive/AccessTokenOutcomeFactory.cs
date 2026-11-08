namespace AStarDev.CloudSync.Accounts.OneDrive;

/// <summary>Creates <see cref="AccessTokenOutcome"/> cases.</summary>
public static class AccessTokenOutcomeFactory
{
    /// <summary>Creates an acquired outcome. A missing token is normalised to an empty string.</summary>
    public static AccessTokenOutcome CreateAcquired(string? token) => new AccessTokenOutcome.Acquired(token ?? string.Empty);

    /// <summary>Creates a sign-in-required outcome.</summary>
    public static AccessTokenOutcome CreateSignInRequired() => new AccessTokenOutcome.SignInRequired();

    /// <summary>Creates an unknown-account outcome.</summary>
    public static AccessTokenOutcome CreateUnknownAccount() => new AccessTokenOutcome.UnknownAccount();

    /// <summary>Creates a token-cache-unavailable outcome.</summary>
    public static AccessTokenOutcome CreateTokenCacheUnavailable() => new AccessTokenOutcome.TokenCacheUnavailable();

    /// <summary>Creates an unavailable outcome.</summary>
    public static AccessTokenOutcome CreateUnavailable() => new AccessTokenOutcome.Unavailable();
}
