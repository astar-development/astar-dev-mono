namespace AStar.Dev.OneDrive.Sync.Client.Domain;

/// <summary>Captures the local and remote file state at the moment a sync conflict was detected.</summary>
public sealed record ConflictSnapshot(DateTimeOffset LocalModified, long LocalSize, DateTimeOffset RemoteModified, long RemoteSize);
