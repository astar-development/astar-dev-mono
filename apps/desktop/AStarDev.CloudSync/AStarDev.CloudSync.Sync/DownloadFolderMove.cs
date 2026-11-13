namespace AStarDev.CloudSync.Sync;

internal sealed record DownloadFolderMove(RelativeSyncPath From, RelativeSyncPath To, bool Blocked);
