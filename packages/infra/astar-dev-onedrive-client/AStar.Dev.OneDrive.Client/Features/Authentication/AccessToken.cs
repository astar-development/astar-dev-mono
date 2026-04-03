namespace AStar.Dev.OneDrive.Client.Features.Authentication;

/// <summary>
///     Represents an OAuth access token with expiration details.
/// </summary>
public sealed record AccessToken(string Token, DateTimeOffset ExpiresAt)
{
    /// <summary>Check if token has expired.</summary>
    public bool IsExpired => DateTimeOffset.UtcNow >= ExpiresAt;

    /// <summary>Check if token is expiring soon (within 5 minutes).</summary>
    public bool IsExpiringSoon => DateTimeOffset.UtcNow.AddMinutes(5) >= ExpiresAt;
}
