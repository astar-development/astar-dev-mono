namespace AStarDev.CloudSync.Sync;

/// <summary>At app start, finds the runs the app died in, marks them interrupted, tells the user in the activity feed and starts a normal pass with <see cref="SyncTrigger.Recovery"/> for each account, spread out as the scheduler spreads its passes (#1610).</summary>
public interface IInterruptedSyncRecovery
{
    /// <summary>Recovers every interrupted run. Call it once, before any pass of this app session starts. A journal that cannot be read recovers nothing.</summary>
    /// <param name="cancellationToken">Cancels the recovery passes.</param>
    /// <returns>One outcome per interrupted account; empty when nothing was interrupted.</returns>
    Task<IReadOnlyList<RecoveryOutcome>> RecoverAsync(CancellationToken cancellationToken);
}
