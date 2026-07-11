using AStar.Dev.FunctionalParadigm;
using AStar.Dev.Wallpaper.Scraper.Models;
using FileClassificationCategoryDomain = AStar.Dev.Infrastructure.AppDb.Entities.FileClassificationCategoryEntity;

namespace AStar.Dev.Wallpaper.Scraper.Services;

public interface IFileClassificationCategoryService
{
    Task<Result<List<FileClassificationCategoryDomain>, ScrapeError>> ExportScrapedTagsAsync(CancellationToken cancellationToken);
    Task<Result<int, ScrapeError>> ImportScrapedTagsAsync(IReadOnlyList<FileClassificationCategoryDomain> tags, CancellationToken cancellationToken);
}
