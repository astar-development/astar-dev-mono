using AnotherOneDriveSync.Data;
using AnotherOneDriveSync.Data.Entities;
using Microsoft.Graph;
using Serilog;
using System.Threading;
using Directory = System.IO.Directory;
using File = System.IO.File;
using Path = System.IO.Path;

namespace AnotherOneDriveSync.Core;

public class SyncService : ISyncService
{
    private readonly IGraphService _graphService;
    private readonly SyncDbContext _dbContext;
    private readonly ILogger _logger;
    private readonly TimeSpan _initialRetryDelay;

    public SyncService(IGraphService graphService, SyncDbContext dbContext, ILogger logger, TimeSpan? initialRetryDelay = null)
    {
        _graphService = graphService;
        _dbContext = dbContext;
        _logger = logger;
        _initialRetryDelay = initialRetryDelay ?? TimeSpan.FromSeconds(1);
    }

    public async Task SyncFolderAsync(SyncFolder syncFolder, string localRoot, CancellationToken cancellationToken = default)
    {
        _logger.Information("Starting sync for folder {FolderId} to {LocalPath}", syncFolder.FolderId, syncFolder.LocalPath);

        var driveItem = await _graphService.GetDriveItemMetadataAsync(syncFolder.FolderId);
        if (driveItem == null)
        {
            _logger.Warning("Folder {FolderId} not found in OneDrive", syncFolder.FolderId);
            return;
        }

        if (syncFolder.ETag == driveItem.ETag && syncFolder.LastSyncTime.HasValue &&
            driveItem.LastModifiedDateTime.HasValue && syncFolder.LastSyncTime >= driveItem.LastModifiedDateTime.Value)
        {
            _logger.Information("Folder {FolderId} is up to date", syncFolder.FolderId);
            return;
        }

        var localPath = Path.Combine(localRoot, syncFolder.LocalPath);
        var visited = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        // Use syncFolder.FolderId (not driveItem.Id) for the root listing; in some OneDrive
        // configurations GetDriveItemMetadataAsync returns a canonicalized ID that differs from
        // the stored FolderId, which would cause children to be enumerated under the wrong parent.
        await SyncFolderContentsAsync(driveItem, syncFolder.FolderId, localPath, syncFolder.Id, visited, cancellationToken);

        syncFolder.LastSyncTime = DateTimeOffset.UtcNow;
        syncFolder.ETag = driveItem.ETag;
        _dbContext.SyncFolders.Update(syncFolder);
        await _dbContext.SaveChangesAsync(cancellationToken);

        _logger.Information("Completed sync for folder {FolderId}", syncFolder.FolderId);
    }

    // parentPath = directory that contains this item (file or folder)
    private async Task SyncDriveItemAsync(DriveItem item, string parentPath, int syncFolderId, HashSet<string> visited, CancellationToken cancellationToken)
    {
        if (item.Folder == null)
        {
            _logger.Debug("Item {Name} ({Id}): treating as FILE (Folder facet is null)", item.Name, item.Id);
            await SyncFileAsync(item, parentPath, syncFolderId, cancellationToken);
        }
        else
        {
            _logger.Debug("Item {Name} ({Id}): treating as FOLDER (Folder facet present, childCount={Count})", item.Name, item.Id, item.Folder.ChildCount);
            await SyncFolderContentsAsync(item, item.Id, Path.Combine(parentPath, item.Name), syncFolderId, visited, cancellationToken);
        }
    }

    private async Task SyncFileAsync(DriveItem item, string parentPath, int syncFolderId, CancellationToken cancellationToken)
    {
        var localFilePath = Path.Combine(parentPath, item.Name);

        var existingMetadata = await _dbContext.DriveItemMetadata.FindAsync(new object[] { item.Id }, cancellationToken);
        if (existingMetadata != null &&
            existingMetadata.ETag == item.ETag &&
            existingMetadata.LastModifiedTime == item.LastModifiedDateTime &&
            File.Exists(localFilePath))
        {
            _logger.Debug("File {Name} is up to date", item.Name);
            return;
        }

        Directory.CreateDirectory(parentPath);
        _logger.Information("Downloading {Name} → {Path}", item.Name, localFilePath);
        await DownloadFileWithRetryAsync(item, localFilePath, cancellationToken);

        if (item.CreatedDateTime.HasValue)
            File.SetCreationTimeUtc(localFilePath, item.CreatedDateTime.Value.UtcDateTime);
        if (item.LastModifiedDateTime.HasValue)
            File.SetLastWriteTimeUtc(localFilePath, item.LastModifiedDateTime.Value.UtcDateTime);

        if (existingMetadata == null)
        {
            existingMetadata = new DriveItemMetadata
            {
                Id = item.Id,
                Name = item.Name,
                ParentId = item.ParentReference?.Id,
                IsFolder = false,
                Size = item.Size,
                CreatedTime = item.CreatedDateTime,
                LastModifiedTime = item.LastModifiedDateTime,
                ETag = item.ETag,
                CTag = item.CTag,
                SyncFolderId = syncFolderId
            };
            _dbContext.DriveItemMetadata.Add(existingMetadata);
        }
        else
        {
            existingMetadata.Name = item.Name;
            existingMetadata.ParentId = item.ParentReference?.Id;
            existingMetadata.Size = item.Size;
            existingMetadata.CreatedTime = item.CreatedDateTime;
            existingMetadata.LastModifiedTime = item.LastModifiedDateTime;
            existingMetadata.ETag = item.ETag;
            existingMetadata.CTag = item.CTag;
            _dbContext.DriveItemMetadata.Update(existingMetadata);
        }
        await _dbContext.SaveChangesAsync(cancellationToken);

        _logger.Information("Downloaded file {Name}", item.Name);
    }

    // localPath = the full path of this folder on disk; listingId = OneDrive item ID to enumerate children of
    private async Task SyncFolderContentsAsync(DriveItem item, string listingId, string localPath, int syncFolderId, HashSet<string> visited, CancellationToken cancellationToken)
    {
        if (!visited.Add(listingId))
        {
            _logger.Warning("Cycle detected for folder {FolderId} at {LocalPath}; skipping", listingId, localPath);
            return;
        }

        Directory.CreateDirectory(localPath);

        if (item.CreatedDateTime.HasValue)
            Directory.SetCreationTimeUtc(localPath, item.CreatedDateTime.Value.UtcDateTime);
        if (item.LastModifiedDateTime.HasValue)
            Directory.SetLastWriteTimeUtc(localPath, item.LastModifiedDateTime.Value.UtcDateTime);

        var existingMetadata = await _dbContext.DriveItemMetadata.FindAsync(new object[] { item.Id }, cancellationToken);
        if (existingMetadata == null)
        {
            existingMetadata = new DriveItemMetadata
            {
                Id = item.Id,
                Name = item.Name,
                ParentId = item.ParentReference?.Id,
                IsFolder = true,
                CreatedTime = item.CreatedDateTime,
                LastModifiedTime = item.LastModifiedDateTime,
                ETag = item.ETag,
                CTag = item.CTag,
                SyncFolderId = syncFolderId
            };
            _dbContext.DriveItemMetadata.Add(existingMetadata);
        }
        else
        {
            existingMetadata.Name = item.Name;
            existingMetadata.ParentId = item.ParentReference?.Id;
            existingMetadata.CreatedTime = item.CreatedDateTime;
            existingMetadata.LastModifiedTime = item.LastModifiedDateTime;
            existingMetadata.ETag = item.ETag;
            existingMetadata.CTag = item.CTag;
            _dbContext.DriveItemMetadata.Update(existingMetadata);
        }
        await _dbContext.SaveChangesAsync(cancellationToken);

        _logger.Debug("Listing children of {FolderId} ({Name})", listingId, item.Name);
        var childCount = 0;
        await foreach (var child in _graphService.ListFolderChildrenAsync(listingId).WithCancellation(cancellationToken))
        {
            childCount++;
            await SyncDriveItemAsync(child, localPath, syncFolderId, visited, cancellationToken);
        }
        if (childCount == 0)
            _logger.Warning("Folder {FolderId} ({Name}) returned 0 children from API — verify contents in OneDrive", listingId, item.Name);
        else
            _logger.Debug("Folder {FolderId} ({Name}) enumerated {Count} children", listingId, item.Name, childCount);
    }

    private async Task DownloadFileWithRetryAsync(DriveItem item, string localFilePath, CancellationToken cancellationToken)
    {
        const int maxRetries = 3;
        var delay = _initialRetryDelay;

        for (var attempt = 1; attempt <= maxRetries; attempt++)
        {
            try
            {
                await DownloadFileAsync(item, localFilePath, cancellationToken);
                return;
            }
            catch (Exception ex) when (attempt < maxRetries)
            {
                _logger.Warning(ex, "Download attempt {Attempt} failed for {Name}, retrying in {Delay}", attempt, item.Name, delay);
                await Task.Delay(delay, cancellationToken);
                delay = delay * 2;
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "Download failed after {MaxRetries} attempts for {Name}", maxRetries, item.Name);
                throw;
            }
        }
    }

    private async Task DownloadFileAsync(DriveItem item, string localFilePath, CancellationToken cancellationToken)
    {
        var tempFilePath = localFilePath + ".tmp";

        try
        {
            using var response = await _graphService.DownloadItemContentAsync(item.Id);
            using var fileStream = File.Create(tempFilePath);
            await response.CopyToAsync(fileStream, cancellationToken);
        }
        catch
        {
            if (File.Exists(tempFilePath))
                File.Delete(tempFilePath);
            throw;
        }

        if (File.Exists(localFilePath))
            File.Delete(localFilePath);
        File.Move(tempFilePath, localFilePath);
    }
}
