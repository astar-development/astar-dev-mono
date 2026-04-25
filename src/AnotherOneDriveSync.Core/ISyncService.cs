using AnotherOneDriveSync.Data.Entities;

namespace AnotherOneDriveSync.Core;

public interface ISyncService
{
    Task SyncFolderAsync(SyncFolder syncFolder, string localRoot, CancellationToken cancellationToken = default);
}
