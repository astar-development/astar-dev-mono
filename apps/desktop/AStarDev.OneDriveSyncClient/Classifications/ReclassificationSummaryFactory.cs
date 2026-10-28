namespace AStarDev.OneDriveSyncClient.Classifications;

/// <summary>Creates <see cref="ReclassificationSummary"/> values.</summary>
public static class ReclassificationSummaryFactory
{
    /// <summary>Creates a summary, normalising negative counts to zero.</summary>
    public static ReclassificationSummary Create(int filesProcessed, int classificationsAdded) => new(Math.Max(0, filesProcessed), Math.Max(0, classificationsAdded));
}
