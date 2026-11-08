namespace AStarDev.CloudSync.Sync;

/// <summary>Whether a pair's local deletions may go ahead in this pass.</summary>
public abstract record MassDeletionVerdict
{
    private MassDeletionVerdict()
    {
    }

    /// <summary>The deletions may be planned.</summary>
    public sealed record Allowed : MassDeletionVerdict
    {
        internal Allowed()
        {
        }
    }

    /// <summary>Too many items are missing at once; the pair's deletions are held for the user to confirm.</summary>
    public sealed record Held : MassDeletionVerdict
    {
        internal Held()
        {
        }
    }
}
