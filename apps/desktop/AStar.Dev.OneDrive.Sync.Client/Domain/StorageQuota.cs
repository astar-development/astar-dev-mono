namespace AStar.Dev.OneDrive.Sync.Client.Domain;

/// <summary>Groups the OneDrive storage quota fields refreshed as a pair from the Graph API.</summary>
public sealed record StorageQuota(long TotalBytes, long UsedBytes);
