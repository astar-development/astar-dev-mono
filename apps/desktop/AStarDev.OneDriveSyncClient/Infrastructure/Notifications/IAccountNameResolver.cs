namespace AStarDev.OneDriveSyncClient.Infrastructure.Notifications;

/// <summary>Resolves the display name of an account for use in notifications.</summary>
public interface IAccountNameResolver
{
    /// <summary>Returns the display name for the account, or the raw identifier when the account is unknown.</summary>
    string Resolve(string accountId);
}
