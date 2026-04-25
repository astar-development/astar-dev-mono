using Microsoft.Graph;

namespace AnotherOneDriveSync.Core;

public interface IGraphService
{
    IAsyncEnumerable<DriveItem> ListDriveRootChildrenAsync();
    IAsyncEnumerable<DriveItem> ListFolderChildrenAsync(string folderId);
    Task<DriveItem> GetDriveItemMetadataAsync(string itemId);
    Task<Stream> DownloadItemContentAsync(string itemId);
}
