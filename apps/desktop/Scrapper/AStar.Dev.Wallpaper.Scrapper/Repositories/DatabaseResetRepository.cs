using AStar.Dev.Infrastructure.FilesDb.Data;
using AStar.Dev.Infrastructure.FilesDb.Models;
using Microsoft.EntityFrameworkCore;

namespace AStar.Dev.Wallpaper.Scrapper.Repositories;

public sealed class DatabaseResetRepository(IDbContextFactory<FilesContext> contextFactory) : IDatabaseResetRepository
{
    public async Task ResetSearchCategoriesAsync(CancellationToken cancellationToken = default)
    {
        await using var context = await contextFactory.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);
        _ = await context.Set<SearchCategories>()
            .ExecuteUpdateAsync(
                s => s.SetProperty(c => c.LastKnownImageCount, 0)
                      .SetProperty(c => c.LastPageVisited, 0)
                      .SetProperty(c => c.TotalPages, 0)
                      .SetProperty(c => c.IncludeInSearch, true),
                cancellationToken)
            .ConfigureAwait(false);
    }

    public async Task DeleteAllFilesAsync(CancellationToken cancellationToken = default)
    {
        await using var context = await contextFactory.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);
        _ = await context.Files.ExecuteDeleteAsync(cancellationToken).ConfigureAwait(false);
    }

    public async Task<string?> GetBaseSaveDirectoryAsync(CancellationToken cancellationToken = default)
    {
        await using var context = await contextFactory.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);
        var dirs = await context.Set<ScrapeDirectories>()
            .OrderByDescending(d => d.Id)
            .FirstOrDefaultAsync(cancellationToken)
            .ConfigureAwait(false);

        return dirs?.BaseSaveDirectory;
    }
}
