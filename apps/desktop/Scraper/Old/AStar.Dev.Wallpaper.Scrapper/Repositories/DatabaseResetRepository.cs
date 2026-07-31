using AStar.Dev.Infrastructure.AppDb;
using AStar.Dev.Infrastructure.AppDb.Entities;
using Microsoft.EntityFrameworkCore;

namespace AStar.Dev.Wallpaper.Scrapper.Repositories;

public sealed class DatabaseResetRepository(IDbContextFactory<AppDbContext> contextFactory) : IDatabaseResetRepository
{
    public async Task ResetSearchCategoriesAsync(CancellationToken cancellationToken = default)
    {
        await using var context = await contextFactory.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);
        _ = await context.Set<SearchCategoryEntity>()
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
        var dirs = await context.Set<ScrapeDirectoriesEntity>()
            .OrderByDescending(d => d.Id)
            .FirstOrDefaultAsync(cancellationToken)
            .ConfigureAwait(false);

        return dirs?.BaseSaveDirectory;
    }
}
