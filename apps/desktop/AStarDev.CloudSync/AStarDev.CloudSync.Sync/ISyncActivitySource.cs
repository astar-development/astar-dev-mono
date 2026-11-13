using AStarDev.CloudSync.Accounts;

namespace AStarDev.CloudSync.Sync;

/// <summary>Tells which accounts are syncing and raises an event when a run starts or ends (#1402). The overview, tray, notifications and updater watch it.</summary>
public interface ISyncActivitySource
{
    /// <summary>Raised when a run starts, on the thread that runs it.</summary>
    event EventHandler<SyncPassStartedEventArgs>? PassStarted;

    /// <summary>Raised when a run ends, after the account is free again, on the thread that ran it.</summary>
    event EventHandler<SyncPassFinishedEventArgs>? PassFinished;

    /// <summary>Whether a run is in progress for the account.</summary>
    /// <param name="accountId">The account.</param>
    AccountSyncActivity ActivityOf(AccountId accountId);
}
