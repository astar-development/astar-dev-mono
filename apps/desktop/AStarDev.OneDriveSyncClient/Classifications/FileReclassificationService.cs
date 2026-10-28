using AStarDev.FunctionalParadigm;
using AStar.Dev.Infrastructure.AppDb;
using AStar.Dev.Infrastructure.AppDb.Domain;
using AStarDev.OneDriveSyncClient.Data.Repositories;
using Microsoft.EntityFrameworkCore;

namespace AStarDev.OneDriveSyncClient.Classifications;

/// <inheritdoc />
public sealed class FileReclassificationService(IDbContextFactory<AppDbContext> dbContextFactory, IFileClassificationRepository fileClassificationRepository, IFileAutoCategorisor fileAutoCategorisor, IFileTypeCategoriser fileTypeCategoriser, IFolderNameCategoriser folderNameCategoriser, ICategoryResolutionService categoryResolutionService) : IFileReclassificationService
{
    /// <inheritdoc />
    public Task<Result<ReclassificationSummary, string>> ReclassifyAllAsync(IProgress<ReclassificationProgress> progress, CancellationToken cancellationToken = default)
    {
        _ = (dbContextFactory, fileClassificationRepository, fileAutoCategorisor, fileTypeCategoriser, folderNameCategoriser, categoryResolutionService);

        return Task.FromResult<Result<ReclassificationSummary, string>>(new Fail<ReclassificationSummary, string>("Not implemented"));
    }
}
