namespace AStarDev.CloudSync.Sync;

/// <summary>How the user settles a conflict (CR-03, #1406).</summary>
public abstract record ConflictStrategy
{
    private ConflictStrategy()
    {
    }

    /// <summary>The local side is kept and replaces the cloud side.</summary>
    public sealed record LocalWins : ConflictStrategy
    {
        internal LocalWins()
        {
        }
    }

    /// <summary>The cloud side is kept and replaces the local side.</summary>
    public sealed record RemoteWins : ConflictStrategy
    {
        internal RemoteWins()
        {
        }
    }

    /// <summary>Both sides are kept; the local copy gets a Keep Both name (CR-04).</summary>
    public sealed record KeepBoth : ConflictStrategy
    {
        internal KeepBoth()
        {
        }
    }

    /// <summary>Nothing changes; the item stays in the queue (CR-05).</summary>
    public sealed record Skip : ConflictStrategy
    {
        internal Skip()
        {
        }
    }
}
