namespace AStarDev.CloudSync.Classification;

/// <summary>Whether the automatic links come from the current engine and seed.</summary>
public abstract record ReclassifyFreshness
{
    private ReclassifyFreshness()
    {
    }

    /// <summary>The last completed run used the current engine and seed versions.</summary>
    public sealed record UpToDate : ReclassifyFreshness
    {
        internal UpToDate()
        {
        }
    }

    /// <summary>No run has completed yet, or the engine or seed version changed since the last one, so a reclassify is worth offering.</summary>
    public sealed record Stale : ReclassifyFreshness
    {
        internal Stale()
        {
        }
    }
}
