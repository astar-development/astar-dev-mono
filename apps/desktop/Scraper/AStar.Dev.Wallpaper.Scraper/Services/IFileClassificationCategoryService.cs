using FileClassificationCategoryDomain = AStar.Dev.Infrastructure.AppDb.Entities.FileClassificationCategoryEntity;

namespace AStar.Dev.Wallpaper.Scraper.Services;

public interface IFileClassificationCategoryService
{
    Task<List<FileClassificationCategoryDomain>> ExportScrapedTagsAsync(CancellationToken cancellationToken);
    Task<int> ImportScrapedTagsAsync(IReadOnlyList<FileClassificationCategoryDomain> tags, CancellationToken cancellationToken);
}
