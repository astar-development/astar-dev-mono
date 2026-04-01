namespace AStar.Dev.OneDrive.Client.Features.Authentication;

/// <summary>
///     Represents the authentication state of an account (AU-05).
/// </summary>
public enum AccountAuthState
{
    /// <summary>Account has valid tokens; syncs can proceed.</summary>
    Authenticated,

    /// <summary>Token refresh failed; user must re-authenticate via browser.</summary>
    AuthRequired,
}
