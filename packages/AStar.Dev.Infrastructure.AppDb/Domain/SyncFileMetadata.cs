using AStar.Dev.FunctionalParadigm;
using AStar.Dev.Infrastructure.AppDb.Entities;

namespace AStar.Dev.Infrastructure.AppDb.Domain;

/// <summary>Remote file attributes captured at the time the sync job was queued.</summary>
public sealed record SyncFileMetadata(long FileSize, DateTimeOffset RemoteModified, Option<VersionInfo> VersionInfo);
