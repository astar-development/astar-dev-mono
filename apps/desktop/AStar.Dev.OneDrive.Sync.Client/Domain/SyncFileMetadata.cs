using AStar.Dev.Functional.Extensions;
using AStar.Dev.OneDrive.Sync.Client.Data.Entities;

namespace AStar.Dev.OneDrive.Sync.Client.Domain;

/// <summary>Remote file attributes captured at the time the sync job was queued.</summary>
public sealed record SyncFileMetadata(long FileSize, DateTimeOffset RemoteModified, Option<VersionInfo> VersionInfo);
