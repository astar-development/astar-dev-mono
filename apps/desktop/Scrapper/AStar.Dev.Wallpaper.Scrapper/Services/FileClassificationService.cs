using System.Diagnostics.CodeAnalysis;
using AStar.Dev.Infrastructure.AppDb.Entities;

namespace AStar.Dev.Wallpaper.Scrapper.Services;

// TODO(#697): rewrite against the FileClassificationCategoryEntity/FileClassificationKeywordEntity
// hierarchy. Stubbed as a no-op during #696's mechanical AppDbContext migration, since the old
// flat FileClassification model (with an owned Keywords collection) has no equivalent shape here.
// contextFactory/timeProvider will return to the constructor once the real implementation lands.
public sealed class FileClassificationService
{
    [SuppressMessage("Performance", "CA1822:Mark members as static", Justification = "Instance API kept intentionally; #697's rewrite will need instance state again.")]
    public Task<PageClassificationData> LoadPageClassificationDataAsync(string categoryId, CancellationToken token)
        => Task.FromResult(PageClassificationDataFactory.Create([], null, []));

    [SuppressMessage("Performance", "CA1822:Mark members as static", Justification = "Instance API kept intentionally; #697's rewrite will need instance state again.")]
    public Task ClassifyAsync(FileDetailEntity fileDetail, PageClassificationData pageData, IReadOnlyList<string> imageTags, CancellationToken token)
        => Task.CompletedTask;

    [SuppressMessage("Performance", "CA1822:Mark members as static", Justification = "Instance API kept intentionally; #697's rewrite will need instance state again.")]
    internal Task<List<FileClassificationCategoryEntity>> ExportClassificationsAsync(CancellationToken token)
        => Task.FromResult(new List<FileClassificationCategoryEntity>());

    [SuppressMessage("Performance", "CA1822:Mark members as static", Justification = "Instance API kept intentionally; #697's rewrite will need instance state again.")]
    internal Task<object> ImportClassificationsAsync(List<FileClassificationCategoryEntity> classifications, CancellationToken token)
        => Task.FromResult<object>(new { Success = false, Count = 0 });
}
