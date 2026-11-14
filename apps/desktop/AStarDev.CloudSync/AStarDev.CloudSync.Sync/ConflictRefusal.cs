namespace AStarDev.CloudSync.Sync;

/// <summary>Why <see cref="IConflictResolver"/> refused a whole call before resolving anything (#1623). Every conflict stays open.</summary>
public abstract record ConflictRefusal
{
    private ConflictRefusal()
    {
    }

    /// <summary>A sync pass or another resolution is running for the account.</summary>
    public sealed record Busy : ConflictRefusal
    {
        internal Busy()
        {
        }
    }

    /// <summary>Sync is paused for an update or an account removal.</summary>
    public sealed record Draining : ConflictRefusal
    {
        internal Draining()
        {
        }
    }

    /// <summary>The open conflicts could not be read, so nothing was attempted.</summary>
    public sealed record QueueUnreadable : ConflictRefusal
    {
        internal QueueUnreadable()
        {
        }
    }
}
