using AStar.Dev.Wallpaper.Scraper.Repositories;
using ScrapedTagDomain = AStar.Dev.Infrastructure.AppDb.Entities.ScrapedTagEntity;

namespace AStar.Dev.Wallpaper.Scraper.Services;

public sealed class ScrapedTagService(IScrapedTagRepository repository) : IScrapedTagService
{
    public Task<List<ScrapedTagDomain>> ExportScrapedTagsAsync(CancellationToken ct)
        => repository.GetAllAsync(ct);

    public async Task<int> ImportScrapedTagsAsync(IReadOnlyList<ScrapedTagDomain> tags, CancellationToken ct)
    {
        await repository.UpsertAsync(tags, ct);

        return tags.Count;
    }
}
