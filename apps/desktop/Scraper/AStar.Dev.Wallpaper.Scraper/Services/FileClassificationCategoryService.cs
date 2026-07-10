using AStar.Dev.Wallpaper.Scraper.Repositories;
using FileClassificationCategoryDomain = AStar.Dev.Infrastructure.AppDb.Entities.FileClassificationCategoryEntity;

namespace AStar.Dev.Wallpaper.Scraper.Services;

public sealed class FileClassificationCategoryService(IFileClassificationCategoriesRepository repository) : IFileClassificationCategoryService
{
    public Task<List<FileClassificationCategoryDomain>> ExportScrapedTagsAsync(CancellationToken cancellationToken)
        => repository.GetAllAsync(cancellationToken);

    public async Task<int> ImportScrapedTagsAsync(IReadOnlyList<FileClassificationCategoryDomain> tags, CancellationToken cancellationToken)
    {
        await repository.UpsertAsync(tags, cancellationToken);

        return tags.Count;
    }
}
