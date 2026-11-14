namespace AStarDev.CloudSync.Sync;

/// <summary>Whether the operating system reports a network (#1462).</summary>
public abstract record NetworkAvailability
{
    private NetworkAvailability()
    {
    }

    /// <summary>The operating system reports a network.</summary>
    public sealed record Available : NetworkAvailability
    {
        internal Available()
        {
        }
    }

    /// <summary>The operating system reports no network.</summary>
    public sealed record Unavailable : NetworkAvailability
    {
        internal Unavailable()
        {
        }
    }
}
