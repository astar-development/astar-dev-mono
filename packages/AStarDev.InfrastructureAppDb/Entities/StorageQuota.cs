namespace AStar.Dev.Infrastructure.AppDb.Entities;

/// <summary>
/// Represents the storage quota information for a OneDrive account, including total storage capacity and used storage. This entity can be used to track and display the storage usage of the account within the sync client application, allowing users to manage their storage effectively and make informed decisions about their synchronization needs.
/// </summary>
/// <param name="TotalBytes">The total storage capacity in bytes.</param>
/// <param name="UsedBytes">The amount of storage currently used in bytes.</param>
public sealed record StorageQuota(long TotalBytes, long UsedBytes);
