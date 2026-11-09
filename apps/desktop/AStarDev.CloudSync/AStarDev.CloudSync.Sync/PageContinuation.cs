namespace AStarDev.CloudSync.Sync;

/// <summary>What follows a page of the change feed.</summary>
public abstract record PageContinuation
{
    private PageContinuation()
    {
    }

    /// <summary>Another page follows; ask again with <see cref="Next"/>.</summary>
    public sealed record MorePages : PageContinuation
    {
        internal MorePages(ChangeCursor next)
        {
            Next = next;
        }

        /// <summary>The cursor for the next page.</summary>
        public ChangeCursor Next { get; }
    }

    /// <summary>The feed is up to date; keep <see cref="Delta"/> for the next incremental pass.</summary>
    public sealed record CaughtUp : PageContinuation
    {
        internal CaughtUp(ChangeCursor delta)
        {
            Delta = delta;
        }

        /// <summary>The cursor for the next incremental pass.</summary>
        public ChangeCursor Delta { get; }
    }
}
