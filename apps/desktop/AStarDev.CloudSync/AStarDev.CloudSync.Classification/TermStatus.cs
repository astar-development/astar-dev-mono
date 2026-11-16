namespace AStarDev.CloudSync.Classification;

/// <summary>Whether a term takes part in classification.</summary>
public abstract record TermStatus
{
    private TermStatus()
    {
    }

    /// <summary>The term classifies files.</summary>
    public sealed record Active : TermStatus
    {
        internal Active()
        {
        }
    }

    /// <summary>The user ignored the term. It is kept, never deleted, so it cannot come back on a later sync or seed.</summary>
    public sealed record Ignored : TermStatus
    {
        internal Ignored()
        {
        }
    }
}
