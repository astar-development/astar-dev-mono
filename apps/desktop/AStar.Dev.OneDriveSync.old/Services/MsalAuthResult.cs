namespace AStar.Dev.OneDriveSync.old.Services;

/// <summary>AM-01: Result of an MSAL interactive sign-in.</summary>
public sealed record MsalAuthResult(string AccountId, string Email, string DisplayName, string AccessToken);
