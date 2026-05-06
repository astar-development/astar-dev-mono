namespace AStar.Dev.OneDrive.Sync.Client.Domain;

/// <summary>Groups the two fields that govern how an account syncs locally.</summary>
public sealed record AccountSyncConfig(ConflictPolicy ConflictPolicy, LocalSyncPath LocalSyncPath);
