namespace AStarDev.CloudSync.Sync;

/// <summary>Why the database health check failed (#1612).</summary>
public abstract record DatabaseHealthFailure
{
    private DatabaseHealthFailure()
    {
    }

    /// <summary>The integrity check ran and reported damage.</summary>
    public sealed record Damaged : DatabaseHealthFailure
    {
        internal Damaged()
        {
        }
    }

    /// <summary>The database could not be opened or read at all.</summary>
    public sealed record Unreadable : DatabaseHealthFailure
    {
        internal Unreadable()
        {
        }
    }
}
