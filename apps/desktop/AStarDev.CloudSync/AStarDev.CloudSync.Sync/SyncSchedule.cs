namespace AStarDev.CloudSync.Sync;

/// <summary>How often every account syncs by itself (SE-04, #1403). One schedule serves the whole app.</summary>
public abstract record SyncSchedule
{
    private SyncSchedule()
    {
    }

    /// <summary>Every account syncs once per interval; the accounts' starts are staggered across it (SE-05).</summary>
    public sealed record Every : SyncSchedule
    {
        internal Every(TimeSpan interval)
        {
            Interval = interval;
        }

        /// <summary>The time between two syncs of one account.</summary>
        public TimeSpan Interval { get; }
    }

    /// <summary>No automatic syncs: the user presses Sync Now.</summary>
    public sealed record ManualOnly : SyncSchedule
    {
        internal ManualOnly()
        {
        }
    }
}
