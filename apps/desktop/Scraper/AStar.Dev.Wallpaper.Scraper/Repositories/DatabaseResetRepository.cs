using AStar.Dev.FunctionalParadigm;
using AStar.Dev.Infrastructure.AppDb;
using AStar.Dev.Infrastructure.AppDb.Entities;
using AStar.Dev.Utilities;
using AStar.Dev.Wallpaper.Scraper.Models;
using Microsoft.EntityFrameworkCore;

namespace AStar.Dev.Wallpaper.Scraper.Repositories;

public sealed class DatabaseResetRepository(IDbContextFactory<AppDbContext> contextFactory) : IDatabaseResetRepository
{
    public async Task<Result<Unit, ScrapeError>> ResetSearchCategoriesAsync(CancellationToken cancellationToken = default)
        => (await Try.RunAsync(async () =>
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

                return Unit.Instance;
            }).ConfigureAwait(false))
            .ToResult(exception => (ScrapeError)ScrapeErrorFactory.CreateRepositoryOperationFailed(nameof(ResetSearchCategoriesAsync), exception.Message));

    public async Task<Result<Unit, ScrapeError>> DeleteAllFilesAsync(CancellationToken cancellationToken = default)
        => (await Try.RunAsync(async () =>
            {
                await using var context = await contextFactory.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);
                _ = await context.Files.ExecuteDeleteAsync(cancellationToken).ConfigureAwait(false);

                return Unit.Instance;
            }).ConfigureAwait(false))
            .ToResult(exception => (ScrapeError)ScrapeErrorFactory.CreateRepositoryOperationFailed(nameof(DeleteAllFilesAsync), exception.Message));

    public async Task<Result<Option<string>, ScrapeError>> GetBaseSaveDirectoryAsync(CancellationToken cancellationToken = default)
        => (await Try.RunAsync(async () =>
            {
                await using var context = await contextFactory.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);
                var dirs = await context.Set<ScrapeDirectoriesEntity>()
                    .OrderByDescending(d => d.Id)
                    .FirstOrDefaultAsync(cancellationToken)
                    .ConfigureAwait(false);

                Option<string> baseSaveDirectory = dirs is null ? Option.None<string>() : Option.Some(dirs.RootDirectory.CombinePath(dirs.BaseSaveDirectory));

                return baseSaveDirectory;
            }).ConfigureAwait(false))
            .ToResult(exception => (ScrapeError)ScrapeErrorFactory.CreateRepositoryOperationFailed(nameof(GetBaseSaveDirectoryAsync), exception.Message));
}
