using AStarDev.FunctionalParadigm;
using AStar.Dev.Infrastructure.AppDb;
using Microsoft.EntityFrameworkCore;

namespace AStarDev.OneDriveSyncClient.Classifications;

/// <inheritdoc />
public sealed class DefaultCategorySeedingService(IDbContextFactory<AppDbContext> dbContextFactory) : IDefaultCategorySeedingService
{
    /// <inheritdoc />
    public Task<Result<int, string>> SeedDefaultCategoriesAsync(CancellationToken cancellationToken = default)
    {
        _ = dbContextFactory;

        return Task.FromResult<Result<int, string>>(new Ok<int, string>(0));
    }
}
