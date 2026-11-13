namespace AStarDev.CloudSync.Sync;

/// <summary>What started a sync run for an account (#1402).</summary>
public abstract record SyncTrigger
{
    private SyncTrigger()
    {
    }

    /// <summary>The user pressed Sync Now.</summary>
    public sealed record Manual : SyncTrigger
    {
        internal Manual()
        {
        }
    }

    /// <summary>The scheduler started the run (#1403).</summary>
    public sealed record Scheduled : SyncTrigger
    {
        internal Scheduled()
        {
        }
    }

    /// <summary>The user started the run from the system tray (#1409).</summary>
    public sealed record Tray : SyncTrigger
    {
        internal Tray()
        {
        }
    }

    /// <summary>The run resumes work after a crash or restart (#1407).</summary>
    public sealed record Recovery : SyncTrigger
    {
        internal Recovery()
        {
        }
    }
}
