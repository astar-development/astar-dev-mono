using AStarDev.FunctionalParadigm;
using AStar.Dev.Infrastructure.AppDb;
using AStar.Dev.Infrastructure.AppDb.Domain;
using Microsoft.EntityFrameworkCore;

namespace AStarDev.OneDriveSyncClient.Classifications;

/// <inheritdoc />
public sealed class PersonCategoryCleanupService(IDbContextFactory<AppDbContext> dbContextFactory, IFileAutoCategorisor fileAutoCategorisor) : IPersonCategoryCleanupService
{
    /// <inheritdoc />
    public Task<Result<int, string>> RemoveInvalidPersonCategoriesAsync(CancellationToken cancellationToken = default)
    {
        _ = dbContextFactory;
        _ = fileAutoCategorisor;

        return Task.FromResult<Result<int, string>>(new Ok<int, string>(0));
    }
}
