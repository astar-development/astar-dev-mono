namespace AStar.Dev.OneDrive.Sync.Client.Data.Entities;

/// <summary>
/// Represents version information for a OneDrive item, including ETag and CTag values. The ETag is used to track changes to the content of the item, while the CTag is used to track changes to the item's metadata. This entity allows the sync client to efficiently determine if an item has changed since the last synchronization, enabling it to make informed decisions about whether to synchronize the item or not. Both properties are nullable to accommodate cases where version information may not be available or applicable, such as for new items that have not yet been synchronized or for items that do not support versioning.
/// </summary>
/// <param name="ETag">The ETag for the item.</param>
/// <param name="CTag">The CTag for the item.</param>
public sealed record VersionInfo(string? ETag, string? CTag);
