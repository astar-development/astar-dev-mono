namespace AStar.Dev.OneDrive.Sync.Client.Infrastructure.Authentication;

/// <summary>Base type for all authentication error cases.</summary>
public abstract record AuthError;

/// <summary>The user cancelled the authentication flow.</summary>
public sealed record AuthCancelledError : AuthError;

/// <summary>Authentication failed with a descriptive message.</summary>
public sealed record AuthFailedError(string Message) : AuthError;
