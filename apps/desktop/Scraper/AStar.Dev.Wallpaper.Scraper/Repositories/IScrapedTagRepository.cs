using ScrapedTagDomain = AStar.Dev.Infrastructure.AppDb.Entities.ScrapedTagEntity;

namespace AStar.Dev.Wallpaper.Scraper.Repositories;

public interface IScrapedTagRepository
{
    Task SaveAsync(IReadOnlyList<TagData> tags);
    Task<List<ScrapedTagDomain>> GetAllAsync(CancellationToken ct);
    Task UpsertAsync(IReadOnlyList<ScrapedTagDomain> tags, CancellationToken ct);
}
