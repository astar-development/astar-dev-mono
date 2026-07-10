using AStar.Dev.Infrastructure.AppDb.Entities;
using ScrapedTagDto = AStar.Dev.Wallpaper.Scraper.DTOs.ScrapedTag;

namespace AStar.Dev.Wallpaper.Scraper.Repositories;

/// <summary>
///  Provides extension methods for converting between <see cref="ScrapedTagDto" /> and <see cref="FileClassificationCategoryEntity" />.
/// </summary>
public static class ScrapedTagExtensions
{
    /// <summary>
    ///   Converts a <see cref="ScrapedTagDto" /> to a <see cref="FileClassificationCategoryEntity" />.
    /// </summary>
    /// <param name="tag">The <see cref="ScrapedTagDto" /> to convert.</param>
    /// <returns>A <see cref="FileClassificationCategoryEntity" /> representing the converted tag.</returns>
    public static FileClassificationCategoryEntity ToDomain(this ScrapedTagDto tag)
        => new()
        {
            Id = tag.Id.Value,
            Name = tag.Value,
            Level = tag.Level,
            IsFamous = tag.IsFamous,
            IsInternet = tag.IsInternet,
            IncludeInSearch = tag.IncludeInSearch
        };
}
