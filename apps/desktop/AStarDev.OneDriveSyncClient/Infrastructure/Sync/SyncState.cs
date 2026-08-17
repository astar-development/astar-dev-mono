namespace AStarDev.OneDriveSyncClient.Infrastructure.Sync;

public enum SyncState { Idle, Syncing, Pending, Conflict, Error, Completed, NoSyncPathConfigured, ReAuthRequired }
