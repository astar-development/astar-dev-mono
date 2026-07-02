using ScrapedTagDomain = AStar.Dev.Infrastructure.FilesDb.Models.ScrapedTag;
using ScrapedTagDomainId = AStar.Dev.Infrastructure.FilesDb.Models.ScrapedTagId;
using ScrapedTagDto = AStar.Dev.Wallpaper.Scrapper.DTOs.ScrapedTag;

namespace AStar.Dev.Wallpaper.Scrapper.Repositories;

public static class ScrapedTagExtensions
{
    public static ScrapedTagDomain ToDomain(this ScrapedTagDto tag)
        => new()
        {
            Id = new ScrapedTagDomainId(tag.Id.Value),
            Value = tag.Value,
            Category = tag.Category,
            IncludeInSearch = tag.IncludeInSearch
        };
}
