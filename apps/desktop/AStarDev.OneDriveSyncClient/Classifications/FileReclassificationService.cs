using AStarDev.FunctionalParadigm;
using AStar.Dev.Infrastructure.AppDb;
using AStar.Dev.Infrastructure.AppDb.Domain;
using AStar.Dev.Infrastructure.AppDb.Entities;
using AStarDev.OneDriveSyncClient.Data.Repositories;
using Microsoft.EntityFrameworkCore;

namespace AStarDev.OneDriveSyncClient.Classifications;

/// <inheritdoc />
public sealed class FileReclassificationService(IDbContextFactory<AppDbContext> dbContextFactory, IFileClassificationRepository fileClassificationRepository, IFileAutoCategorisor fileAutoCategorisor, IFileTypeCategoriser fileTypeCategoriser, IFolderNameCategoriser folderNameCategoriser, ICategoryResolutionService categoryResolutionService) : IFileReclassificationService
{
    /// <inheritdoc />
    public async Task<Result<ReclassificationSummary, string>> ReclassifyAllAsync(IProgress<ReclassificationProgress> progress, CancellationToken cancellationToken = default)
    {
        try
        {
            return await Task.Run(() => ReclassifyAsync(progress, cancellationToken), cancellationToken).ConfigureAwait(false);
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            return exception.GetBaseException().Message;
        }
    }

    private async Task<ReclassificationSummary> ReclassifyAsync(IProgress<ReclassificationProgress> progress, CancellationToken cancellationToken)
    {
        var mappings = await fileClassificationRepository.GetAllCategoriesAsync(cancellationToken).ConfigureAwait(false);
        var files = await LoadFilesAsync(cancellationToken).ConfigureAwait(false);
        int added = 0;

        for (int index = 0; index < files.Count; index++)
        {
            cancellationToken.ThrowIfCancellationRequested();
            added += await ReclassifyFileAsync(files[index], mappings, cancellationToken).ConfigureAwait(false);
            progress.Report(ReclassificationProgressFactory.Create(index + 1, files.Count));
        }

        return ReclassificationSummaryFactory.Create(files.Count, added);
    }

    private async Task<IReadOnlyList<(FileId FileDetailId, string RemotePath)>> LoadFilesAsync(CancellationToken cancellationToken)
    {
        await using var db = await dbContextFactory.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);

        var items = await db.SyncedItems
            .AsNoTracking()
            .Where(item => !item.IsFolder && item.FileDetailId != null)
            .Select(item => new { FileDetailId = item.FileDetailId!.Value, item.RemotePath })
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        return [.. items.GroupBy(item => item.FileDetailId).Select(group => (group.Key, group.First().RemotePath))];
    }

    private async Task<int> ReclassifyFileAsync((FileId FileDetailId, string RemotePath) file, IReadOnlyList<FileClassificationCategory> mappings, CancellationToken cancellationToken)
    {
        var categoryIds = await ResolveCategoryIdsAsync(file.RemotePath, mappings, cancellationToken).ConfigureAwait(false);
        var existingIds = await LoadExistingCategoryIdsAsync(file.FileDetailId, cancellationToken).ConfigureAwait(false);
        int[] missingIds = [.. categoryIds.Where(categoryId => !existingIds.Contains(categoryId))];
        await fileClassificationRepository.AddClassificationsAsync(file.FileDetailId, missingIds, cancellationToken).ConfigureAwait(false);

        return missingIds.Length;
    }

    private async Task<IReadOnlyList<int>> ResolveCategoryIdsAsync(string remotePath, IReadOnlyList<FileClassificationCategory> mappings, CancellationToken cancellationToken)
    {
        var analyserResult = fileAutoCategorisor.Categorise(remotePath).Match(c => (IReadOnlyList<FileClassification>)[c], () => []);
        var fileTypeResult = fileTypeCategoriser.Categorise(remotePath).Match(c => (IReadOnlyList<FileClassification>)[c], () => []);
        var classifications = ClassificationCombiner.Combine(FileClassifier.Classify(remotePath, mappings), [.. analyserResult, .. fileTypeResult, .. folderNameCategoriser.Categorise(remotePath)]);

        return await categoryResolutionService.ResolveManyAsync(classifications, cancellationToken).ConfigureAwait(false);
    }

    private async Task<HashSet<int>> LoadExistingCategoryIdsAsync(FileId fileDetailId, CancellationToken cancellationToken)
    {
        await using var db = await dbContextFactory.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);

        var ids = await db.FileClassifications
            .AsNoTracking()
            .Where(link => link.FileDetailId == fileDetailId)
            .Select(link => link.CategoryId)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        return [.. ids];
    }
}
