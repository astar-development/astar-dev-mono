using AStar.Dev.Functional.Extensions;

namespace AStar.Dev.OneDrive.Sync.Client.Data.Entities;

/// <summary>
/// Represents version information for a OneDrive item, including ETag and CTag values.
/// The ETag tracks content changes; the CTag tracks metadata changes.
/// Both are None when version information is not available.
/// </summary>
/// <param name="ETag">The ETag for the item.</param>
/// <param name="CTag">The CTag for the item.</param>
public sealed record VersionInfo(Option<string> ETag, Option<string> CTag);
