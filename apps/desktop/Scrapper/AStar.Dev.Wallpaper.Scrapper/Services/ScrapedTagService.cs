using AStar.Dev.Wallpaper.Scrapper.Repositories;
using ScrapedTagDomain = AStar.Dev.Infrastructure.FilesDb.Models.ScrapedTag;

namespace AStar.Dev.Wallpaper.Scrapper.Services;

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
