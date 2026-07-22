using System.IO.Abstractions;
using AStar.Dev.Infrastructure.AppDb;
using AStar.Dev.Infrastructure.AppDb.Entities;
using AStar.Dev.Wallpaper.Scraper.Services;
using Microsoft.EntityFrameworkCore;

namespace AStar.Dev.Wallpaper.Scraper.Maintenance;

/// <inheritdoc cref="IDatabaseResetService" />
public sealed class DatabaseResetService(IDbContextFactory<AppDbContext> dbContextFactory, IFileSystem fileSystem, Clock clock) : IDatabaseResetService
{
    /// <inheritdoc cref="IDatabaseResetService.ResetDatabaseAsync" />
    public async Task ResetDatabaseAsync(CancellationToken cancellationToken)
    {
        await using var context = await dbContextFactory.CreateDbContextAsync(cancellationToken);

        _ = await context.Files.ExecuteDeleteAsync(cancellationToken);
        _ = await context.FileAccessDetails.ExecuteDeleteAsync(cancellationToken);
        _ = await context.Set<ImageDetailEntity>().ExecuteDeleteAsync(cancellationToken);

        var now = clock();
        _ = await context.SearchCategories.ExecuteUpdateAsync(setters => setters
            .SetProperty(searchCategory => searchCategory.LastKnownImageCount, 0)
            .SetProperty(searchCategory => searchCategory.LastPageVisited, 0)
            .SetProperty(searchCategory => searchCategory.UpdatedAt, now), cancellationToken);
    }

    /// <inheritdoc cref="IDatabaseResetService.RemoveDownloadedFilesAsync" />
    public async Task RemoveDownloadedFilesAsync(CancellationToken cancellationToken)
    {
        await using var context = await dbContextFactory.CreateDbContextAsync(cancellationToken);

        string rootDirectory = await context.ScrapeDirectories.Select(directories => directories.RootDirectory).FirstAsync(cancellationToken);

        if (fileSystem.Directory.Exists(rootDirectory))
        {
            fileSystem.Directory.Delete(rootDirectory, recursive: true);
        }
    }
}
