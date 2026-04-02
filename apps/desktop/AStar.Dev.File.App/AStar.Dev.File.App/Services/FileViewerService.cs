using AStar.Dev.File.App.Data;
using AStar.Dev.File.App.ViewModels;
using Microsoft.EntityFrameworkCore;
using System;
using System.Threading.Tasks;

namespace AStar.Dev.File.App.Services;

/// <summary>
/// Service responsible for file viewing operations, including updating view history.
/// </summary>
public class FileViewerService : IFileViewerService
{
    private readonly IDbContextFactory<FileAppDbContext> _dbContextFactory;

    public event Action<ScannedFileDisplayItem>? FileViewRequested;

    public FileViewerService(IDbContextFactory<FileAppDbContext> dbContextFactory) => _dbContextFactory = dbContextFactory;

    /// <summary>
    /// Processes a file view request by updating the last viewed timestamp in the database
    /// and raising the FileViewRequested event.
    /// </summary>
    /// <param name="item">The file to view. If null, this method returns without action.</param>
    public async Task ViewFileAsync(ScannedFileDisplayItem? item)
    {
        if (item is null)
            return;

        await using var db = await _dbContextFactory.CreateDbContextAsync();
        var file = await db.ScannedFiles.FindAsync(item.Id);
        if (file is not null)
        {
            file.LastViewed = DateTime.UtcNow;
            await db.SaveChangesAsync();
        }

        FileViewRequested?.Invoke(item);
    }
}
