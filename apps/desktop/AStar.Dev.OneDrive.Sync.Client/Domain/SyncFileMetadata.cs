namespace AStar.Dev.OneDrive.Sync.Client.Domain;

/// <summary>Remote file attributes captured at the time the sync job was queued.</summary>
public sealed record SyncFileMetadata(long FileSize, DateTimeOffset RemoteModified);
