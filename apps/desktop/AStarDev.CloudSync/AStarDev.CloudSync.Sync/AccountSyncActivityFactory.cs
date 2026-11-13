namespace AStarDev.CloudSync.Sync;

/// <summary>Creates <see cref="AccountSyncActivity"/> values.</summary>
public static class AccountSyncActivityFactory
{
    /// <summary>Creates the <see cref="AccountSyncActivity.Idle"/> case.</summary>
    public static AccountSyncActivity CreateIdle() => new AccountSyncActivity.Idle();

    /// <summary>Creates the <see cref="AccountSyncActivity.Running"/> case.</summary>
    /// <param name="trigger">What started the run.</param>
    /// <param name="startedAt">When the run started.</param>
    public static AccountSyncActivity CreateRunning(SyncTrigger trigger, DateTimeOffset startedAt) => new AccountSyncActivity.Running(trigger, startedAt);
}
