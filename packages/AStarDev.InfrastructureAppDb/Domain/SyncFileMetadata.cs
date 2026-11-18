using AStar.Dev.Infrastructure.AppDb.Entities;
using AStarDev.FunctionalParadigm;

namespace AStar.Dev.Infrastructure.AppDb.Domain;

/// <summary>Remote file attributes captured at the time the sync job was queued.</summary>
public sealed record SyncFileMetadata(long FileSize, DateTimeOffset RemoteModified, Option<VersionInfo> VersionInfo);
