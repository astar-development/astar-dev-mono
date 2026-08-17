namespace AStar.Dev.Infrastructure.AppDb.Domain;

/// <summary>The local paths involved in a sync operation.</summary>
public sealed record SyncFileTarget(string LocalPath, string RelativePath);
