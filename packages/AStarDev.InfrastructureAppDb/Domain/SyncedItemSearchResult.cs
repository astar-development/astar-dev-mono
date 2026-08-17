using AStar.Dev.Infrastructure.AppDb.Entities;

namespace AStar.Dev.Infrastructure.AppDb.Domain;

/// <summary>A single result from a synced-item search query. Unsynced results represent classified files on disk that no synced item references yet; they carry no remote identity.</summary>
public sealed record SyncedItemSearchResult(int Id, AccountId AccountId, OneDriveItemId RemoteItemId, string RemotePath, string LocalPath, DateTimeOffset RemoteModifiedAt, long? SizeInBytes, IReadOnlyList<string> TagNames, bool IsSynced = true, FileId? FileDetailId = null);
