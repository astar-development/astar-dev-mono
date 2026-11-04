namespace AStarDev.CloudSync.Core.Accounts;

/// <summary>How much of an account's cloud storage is used.</summary>
/// <param name="UsedBytes">Bytes used.</param>
/// <param name="TotalBytes">Bytes available in total.</param>
public sealed record StorageUsage(long UsedBytes, long TotalBytes);
