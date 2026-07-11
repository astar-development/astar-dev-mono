using AStar.Dev.FunctionalParadigm;
using AStar.Dev.Infrastructure.AppDb.Entities;
using AStar.Dev.Wallpaper.Scraper.Models;

namespace AStar.Dev.Wallpaper.Scraper.Repositories;

public interface IFileClassificationCategoriesRepository
{
    Task<Result<Unit, ScrapeError>> SaveAsync(IReadOnlyList<TagData> tags, CancellationToken cancellationToken);
    Task<Result<List<FileClassificationCategoryEntity>, ScrapeError>> GetAllAsync(CancellationToken cancellationToken);
    Task<Result<Unit, ScrapeError>> UpsertAsync(IReadOnlyList<FileClassificationCategoryEntity> tags, CancellationToken cancellationToken);
}
