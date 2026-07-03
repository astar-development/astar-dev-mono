using AStar.Dev.Infrastructure.AppDb.Entities;

namespace AStar.Dev.Infrastructure.AppDb.Domain;

/// <summary>A single result from a synced-item search query.</summary>
public sealed record SyncedItemSearchResult(int Id, AccountId AccountId, OneDriveItemId RemoteItemId, string RemotePath, string LocalPath, DateTimeOffset RemoteModifiedAt, long? SizeInBytes, IReadOnlyList<string> TagNames);
