using AStar.Dev.FunctionalParadigm;
using AStar.Dev.Infrastructure.AppDb;
using AStar.Dev.Infrastructure.AppDb.Entities;
using AStar.Dev.Wallpaper.Scraper.Models;
using Microsoft.EntityFrameworkCore;

namespace AStar.Dev.Wallpaper.Scraper.Repositories;

public sealed class FileDetailRepository(IDbContextFactory<AppDbContext> contextFactory) : IFileDetailRepository
{
    public async Task<Result<bool, ScrapeError>> ExistsAsync(string fileName, CancellationToken cancellationToken)
        => (await Try.RunAsync(async () =>
            {
                await using var context = await contextFactory.CreateDbContextAsync(cancellationToken);

                return await context.Files.AnyAsync(f => f.FileName.Value == fileName, cancellationToken);
            }).ConfigureAwait(false))
            .ToResult(exception => (ScrapeError)ScrapeErrorFactory.CreateRepositoryOperationFailed(nameof(ExistsAsync), exception.Message));

    public async Task<Result<Unit, ScrapeError>> AddAsync(FileDetailEntity fileDetail, CancellationToken cancellationToken)
        => (await Try.RunAsync(async () =>
            {
                await using var context = await contextFactory.CreateDbContextAsync(cancellationToken);

                var handle = FileHandleFactory.Create(fileDetail.FileName.Value ?? fileDetail.FileHandle.Value);
                int existingCount = await context.Files.AsAsyncEnumerable().CountAsync(f => f.FileHandle.Value == handle.Value, cancellationToken);
                if (existingCount > 0)
                    handle = FileHandleFactory.Create($"{handle}-{++existingCount}");
                fileDetail.FileHandle = handle;

                _ = await context.Files.AddAsync(fileDetail, cancellationToken);
                _ = await context.SaveChangesAsync(cancellationToken); // threw here or somewhere after...

                return Unit.Value;
            }).ConfigureAwait(false))
            .ToResult(exception => (ScrapeError)ScrapeErrorFactory.CreateRepositoryOperationFailed(nameof(AddAsync), exception.Message));
}
