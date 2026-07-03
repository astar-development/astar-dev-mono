using ScrapedTagDomain = AStar.Dev.Infrastructure.AppDb.Entities.ScrapedTagEntity;

namespace AStar.Dev.Wallpaper.Scrapper.Services;

public interface IScrapedTagService
{
    Task<List<ScrapedTagDomain>> ExportScrapedTagsAsync(CancellationToken ct);
    Task<int> ImportScrapedTagsAsync(IReadOnlyList<ScrapedTagDomain> tags, CancellationToken ct);
}
