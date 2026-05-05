namespace AStar.Dev.OneDrive.Sync.Client.Domain;

/// <summary>The local paths involved in a sync operation.</summary>
public sealed record SyncFileTarget(string LocalPath, string RelativePath);
