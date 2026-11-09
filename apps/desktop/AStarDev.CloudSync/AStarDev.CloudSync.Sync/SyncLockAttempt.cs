namespace AStarDev.CloudSync.Sync;

/// <summary>The result of asking to start a sync pass for an account.</summary>
public abstract record SyncLockAttempt
{
    private SyncLockAttempt()
    {
    }

    /// <summary>The pass may run; dispose <see cref="Lease"/> when it ends.</summary>
    public sealed record Acquired : SyncLockAttempt
    {
        internal Acquired(SyncLease lease) => Lease = lease;

        /// <summary>The lease to dispose when the pass ends.</summary>
        public SyncLease Lease { get; }
    }

    /// <summary>Another pass is already running for the account.</summary>
    public sealed record Busy : SyncLockAttempt
    {
        internal Busy(SyncPassId runningPass) => RunningPass = runningPass;

        /// <summary>The pass that is running.</summary>
        public SyncPassId RunningPass { get; }
    }

    /// <summary>The lock is drained (an update or an account removal is waiting), so no pass may start.</summary>
    public sealed record Draining : SyncLockAttempt
    {
        internal Draining()
        {
        }
    }
}
