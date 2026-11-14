using AStarDev.CloudSync.Accounts;
using AStarDev.FunctionalParadigm;

namespace AStarDev.CloudSync.Sync;

/// <summary>Tells how far each account's sync run has got (#1563). Updates are throttled to at most four a second for each account, so a large sync cannot flood the UI.</summary>
public interface ISyncProgressFeed
{
    /// <summary>Raised when the progress of a running account is published, on the thread that reported it.</summary>
    event EventHandler<SyncProgressChangedEventArgs>? ProgressChanged;

    /// <summary>The last progress published for the account, or none when no run is in progress.</summary>
    /// <param name="accountId">The account.</param>
    Option<SyncProgress> ProgressOf(AccountId accountId);
}
