using AStarDev.CloudSync.Accounts;

namespace AStarDev.CloudSync.Sync;

/// <summary>Creates <see cref="RecoveryOutcome"/> values.</summary>
public static class RecoveryOutcomeFactory
{
    /// <summary>Creates the <see cref="RecoveryOutcome.Resumed"/> case.</summary>
    /// <param name="accountId">The account.</param>
    public static RecoveryOutcome CreateResumed(AccountId accountId) => new RecoveryOutcome.Resumed(accountId);

    /// <summary>Creates the <see cref="RecoveryOutcome.NotResumed"/> case.</summary>
    /// <param name="accountId">The account.</param>
    /// <param name="reason">Why the recovery pass did not finish.</param>
    public static RecoveryOutcome CreateNotResumed(AccountId accountId, SyncPassFailure reason) => new RecoveryOutcome.NotResumed(accountId, reason);
}
