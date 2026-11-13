namespace AStarDev.CloudSync.Sync;

/// <summary>How many entries wait in an account's attention queue (#1408, #1410).</summary>
public sealed record AttentionCounts
{
    internal AttentionCounts(int conflicts, int heldDeletionSets, int heldDeletionPaths)
    {
        Conflicts = conflicts;
        HeldDeletionSets = heldDeletionSets;
        HeldDeletionPaths = heldDeletionPaths;
    }

    /// <summary>The open conflicts.</summary>
    public int Conflicts { get; }

    /// <summary>The held sets, one per pair and direction.</summary>
    public int HeldDeletionSets { get; }

    /// <summary>The paths across all held sets.</summary>
    public int HeldDeletionPaths { get; }
}
