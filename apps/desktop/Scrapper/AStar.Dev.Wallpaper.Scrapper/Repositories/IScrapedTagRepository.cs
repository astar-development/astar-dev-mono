using ScrapedTagDomain = AStar.Dev.Infrastructure.FilesDb.Models.ScrapedTag;

namespace AStar.Dev.Wallpaper.Scrapper.Repositories;

public interface IScrapedTagRepository
{
    Task SaveAsync(IReadOnlyList<TagData> tags);
    Task<List<ScrapedTagDomain>> GetAllAsync(CancellationToken ct);
    Task UpsertAsync(IReadOnlyList<ScrapedTagDomain> tags, CancellationToken ct);
}
