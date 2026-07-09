using AStar.Dev.Infrastructure.AppDb.Domain;
using AStar.Dev.Infrastructure.AppDb.Entities;
using ScrapedTagDto = AStar.Dev.Wallpaper.Scraper.DTOs.ScrapedTag;

namespace AStar.Dev.Wallpaper.Scraper.Repositories;

public static class ScrapedTagExtensions
{
    public static FileClassificationCategoryEntity ToDomain(this ScrapedTagDto tag)
        => new()
        {
            Id = tag.Id.Value,
            Name = tag.Value,
            Level = tag.Level,
            // need to extract the parent category from the tag's category property
            IsFamous = tag.Category == "Famous",
            IsInternet = tag.Category == "Internet",
            IncludeInSearch = tag.IncludeInSearch
        };
}
