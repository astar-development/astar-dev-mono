using AnotherOneDriveSync.Data.Entities;

namespace AnotherOneDriveSync.App.ViewModels;

public class SyncFolderItemViewModel
{
    public int Id { get; }
    public string DisplayName { get; }
    public string LocalPath { get; }
    public string FolderId { get; }
    public string DriveId { get; }
    public DateTimeOffset? LastSyncTime { get; }

    public string LastSyncTimeDisplay => LastSyncTime.HasValue
        ? $"Last sync: {LastSyncTime.Value.LocalDateTime:g}"
        : "Never synced";

    public SyncFolderItemViewModel(SyncFolder syncFolder)
    {
        Id = syncFolder.Id;
        DisplayName = syncFolder.LocalPath;
        LocalPath = syncFolder.LocalPath;
        FolderId = syncFolder.FolderId;
        DriveId = syncFolder.DriveId;
        LastSyncTime = syncFolder.LastSyncTime;
    }
}
