namespace AStarDev.CloudSync.Sync;

/// <summary>How long account actions wait for a running pass or sign-in to release the account (owner decision, #1393).</summary>
public static class AccountLeasePolicy
{
    /// <summary>The longest wait before an action gives up with "still running".</summary>
    public static readonly TimeSpan WaitForRunningWork = TimeSpan.FromSeconds(10);

    /// <summary>How often the lock is asked again while waiting.</summary>
    public static readonly TimeSpan RetryInterval = TimeSpan.FromMilliseconds(200);
}
