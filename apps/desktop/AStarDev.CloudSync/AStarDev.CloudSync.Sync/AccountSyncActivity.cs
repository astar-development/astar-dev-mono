namespace AStarDev.CloudSync.Sync;

/// <summary>Whether a sync run is in progress for an account (#1402).</summary>
public abstract record AccountSyncActivity
{
    private AccountSyncActivity()
    {
    }

    /// <summary>No run is in progress.</summary>
    public sealed record Idle : AccountSyncActivity
    {
        internal Idle()
        {
        }
    }

    /// <summary>A run is in progress.</summary>
    public sealed record Running : AccountSyncActivity
    {
        internal Running(SyncTrigger trigger, DateTimeOffset startedAt)
        {
            Trigger = trigger;
            StartedAt = startedAt;
        }

        /// <summary>What started the run.</summary>
        public SyncTrigger Trigger { get; }

        /// <summary>When the run started.</summary>
        public DateTimeOffset StartedAt { get; }
    }
}
