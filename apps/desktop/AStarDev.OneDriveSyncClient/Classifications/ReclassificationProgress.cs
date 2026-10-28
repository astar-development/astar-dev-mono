namespace AStarDev.OneDriveSyncClient.Classifications;

/// <summary>How far a reclassification run has got.</summary>
/// <param name="Processed">Files processed so far.</param>
/// <param name="Total">Files to process in total.</param>
public sealed record ReclassificationProgress(int Processed, int Total);
