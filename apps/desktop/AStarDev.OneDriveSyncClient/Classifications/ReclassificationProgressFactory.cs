namespace AStarDev.OneDriveSyncClient.Classifications;

/// <summary>Creates <see cref="ReclassificationProgress"/> values.</summary>
public static class ReclassificationProgressFactory
{
    /// <summary>Creates a progress value, normalising negative counts to zero.</summary>
    public static ReclassificationProgress Create(int processed, int total) => new(Math.Max(0, processed), Math.Max(0, total));
}
