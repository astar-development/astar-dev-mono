using FileClassificationCategoryServiceDomain = AStar.Dev.Infrastructure.AppDb.Entities.FileClassificationCategoryEntity;
using ScrapedTagDto = AStar.Dev.Wallpaper.Scraper.DTOs.ScrapedTag;

namespace AStar.Dev.Wallpaper.Scraper.DTOs;

public static class ScrapedTagExtensions
{
    public static FileClassificationCategoryServiceDomain ToDomain(this ScrapedTagDto dto, TimeProvider timeProvider)
        => new()
        {
            Id = dto.Id.Value,
            Name = dto.Value,
            ParentId = 1, // need to extract the parent category from the tag's category property
            IncludeInSearch = dto.IncludeInSearch,
            CreatedAt = dto.CreatedAt,
            UpdatedAt = timeProvider.GetUtcNow()
        };

    public static List<FileClassificationCategoryServiceDomain> ToDomain(this List<ScrapedTagDto> dtos, TimeProvider timeProvider)
        => [.. dtos.Select(dto => dto.ToDomain(timeProvider))];
}
