namespace AStar.Dev.OneDrive.Sync.Client.Models;

/// <summary>Creates <see cref="SyncJob"/> instances with auto-generated identity fields.</summary>
public static class SyncJobFactory
{
    /// <summary>Creates a new <see cref="SyncJob"/> with a generated <see cref="SyncJob.Id"/> and <see cref="SyncJob.QueuedAt"/> timestamp.</summary>
    public static SyncJob Create(string accountId, string folderId, string remoteItemId, string relativePath, string localPath, SyncDirection direction, long fileSize, DateTimeOffset remoteModified, string? downloadUrl = null)
        => new(accountId, folderId, remoteItemId, relativePath, localPath, direction, fileSize, remoteModified, Guid.NewGuid(), DateTimeOffset.UtcNow, DownloadUrl: downloadUrl);
}
