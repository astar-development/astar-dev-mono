namespace AStarDev.CloudSync.Sync;

/// <inheritdoc />
/// <param name="journal">Finds and marks the interrupted runs.</param>
/// <param name="activity">Tells the user a run was interrupted.</param>
/// <param name="stagger">Spreads the recovery passes.</param>
/// <param name="runner">Runs the recovery passes.</param>
/// <param name="timeProvider">Supplies the clock and the delays.</param>
internal sealed class InterruptedSyncRecovery(ISyncRunJournal journal, IActivityLogWriter activity, IStaggerPolicy stagger, ISyncPassRunner runner, TimeProvider timeProvider) : IInterruptedSyncRecovery
{
    /// <inheritdoc />
    public Task<IReadOnlyList<RecoveryOutcome>> RecoverAsync(CancellationToken cancellationToken) =>
        throw new NotImplementedException($"{journal}{activity}{stagger}{runner}{timeProvider}{cancellationToken}");
}
