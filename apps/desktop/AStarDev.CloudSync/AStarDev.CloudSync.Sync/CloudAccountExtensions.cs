namespace AStarDev.CloudSync.Sync;

/// <summary>Changes to a <see cref="CloudAccount"/>.</summary>
public static class CloudAccountExtensions
{
    /// <summary>Returns the account with the given throttle hint.</summary>
    /// <param name="account">The account.</param>
    /// <param name="throttleHint">The hint to show.</param>
    public static CloudAccount WithThrottleHint(this CloudAccount account, ThrottleHint throttleHint) => new(account.Id, account.DisplayName, account.Email, account.Provider, account.Storage, account.FolderCount, account.FileCount, account.Status, account.LastSync, account.SignIn, throttleHint);
}
