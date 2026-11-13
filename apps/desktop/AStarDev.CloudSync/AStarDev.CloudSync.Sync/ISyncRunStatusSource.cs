using AStarDev.CloudSync.Accounts;
using AStarDev.FunctionalParadigm;

namespace AStarDev.CloudSync.Sync;

/// <summary>Tells what the account's sync runs did, for the status pills (#1402). It follows the runs of this app session only.</summary>
public interface ISyncRunStatusSource
{
    /// <summary>The status the account's runs left: syncing while a run is in progress, otherwise the last run's outcome, or up to date when no run has ended yet. A run skipped because another was in progress, or one that needed sign-in, leaves the status as it was.</summary>
    /// <param name="accountId">The account.</param>
    SyncStatus StatusOf(AccountId accountId);

    /// <summary>When the account's last run finished without failing, if one did.</summary>
    /// <param name="accountId">The account.</param>
    Option<DateTimeOffset> LastSyncOf(AccountId accountId);
}
