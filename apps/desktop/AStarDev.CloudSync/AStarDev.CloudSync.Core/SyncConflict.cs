namespace AStarDev.CloudSync.Core;

/// <summary>A file that changed both locally and in the cloud.</summary>
/// <param name="SyncPairId">The sync pair holding the file.</param>
/// <param name="FileName">The file's name.</param>
/// <param name="DetectedAt">When the conflict was found.</param>
public sealed record SyncConflict(SyncPairId SyncPairId, string FileName, DateTimeOffset DetectedAt);
