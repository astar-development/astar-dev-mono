using AStar.Dev.Infrastructure.AppDb.Entities;

namespace AStar.Dev.Wallpaper.Scraper.Repositories;

public interface IFileClassificationCategoriesRepository
{
    Task SaveAsync(IReadOnlyList<TagData> tags);
    Task<List<FileClassificationCategoryEntity>> GetAllAsync(CancellationToken cancellationToken);
    Task UpsertAsync(IReadOnlyList<FileClassificationCategoryEntity> tags, CancellationToken cancellationToken);
}
