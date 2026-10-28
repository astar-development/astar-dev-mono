namespace AStarDev.OneDriveSyncClient.Classifications;

/// <summary>The outcome of a completed reclassification run.</summary>
/// <param name="FilesProcessed">Files examined.</param>
/// <param name="ClassificationsAdded">New classification links added.</param>
public sealed record ReclassificationSummary(int FilesProcessed, int ClassificationsAdded);
