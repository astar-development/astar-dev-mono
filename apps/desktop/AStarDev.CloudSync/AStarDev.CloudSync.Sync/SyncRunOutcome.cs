namespace AStarDev.CloudSync.Sync;

/// <summary>How a journaled sync run ended (#1610). A run that never wrote its end, because the app died, is marked <see cref="Interrupted"/> when the app next starts.</summary>
public abstract record SyncRunOutcome
{
    private SyncRunOutcome()
    {
    }

    /// <summary>The run finished.</summary>
    public sealed record Completed : SyncRunOutcome
    {
        internal Completed()
        {
        }
    }

    /// <summary>The user or a shutdown cancelled the run.</summary>
    public sealed record Cancelled : SyncRunOutcome
    {
        internal Cancelled()
        {
        }
    }

    /// <summary>The run stopped on a failure.</summary>
    public sealed record Failed : SyncRunOutcome
    {
        internal Failed(SyncFailureReason reason)
        {
            Reason = reason;
        }

        /// <summary>Why the run failed.</summary>
        public SyncFailureReason Reason { get; }
    }

    /// <summary>The app stopped during the run: a crash, a kill or a power loss.</summary>
    public sealed record Interrupted : SyncRunOutcome
    {
        internal Interrupted()
        {
        }
    }
}
