using AStar.Dev.Wallpaper.Scraper.Repositories;
using FileClassificationCategoryDomain = AStar.Dev.Infrastructure.AppDb.Entities.FileClassificationCategoryEntity;

namespace AStar.Dev.Wallpaper.Scraper.Services;

public sealed class FileClassificationCategoryService(IScrapedTagRepository repository) : IFileClassificationCategoryService
{
    public Task<List<FileClassificationCategoryDomain>> ExportScrapedTagsAsync(CancellationToken ct)
        => repository.GetAllAsync(ct);

    public async Task<int> ImportScrapedTagsAsync(IReadOnlyList<FileClassificationCategoryDomain> tags, CancellationToken ct)
    {
        await repository.UpsertAsync(tags, ct);

        return tags.Count;
    }
}
