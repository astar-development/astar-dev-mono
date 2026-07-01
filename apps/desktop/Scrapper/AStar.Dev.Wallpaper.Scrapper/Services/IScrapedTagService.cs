using ScrapedTagDomain = AStar.Dev.Infrastructure.FilesDb.Models.ScrapedTag;

namespace AStar.Dev.Wallpaper.Scrapper.Services;

public interface IScrapedTagService
{
    Task<List<ScrapedTagDomain>> ExportScrapedTagsAsync(CancellationToken ct);
    Task<int> ImportScrapedTagsAsync(IReadOnlyList<ScrapedTagDomain> tags, CancellationToken ct);
}
