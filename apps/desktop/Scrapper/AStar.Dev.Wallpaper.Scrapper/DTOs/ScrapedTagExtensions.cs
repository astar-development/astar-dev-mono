using ScrapedTagDomain = AStar.Dev.Infrastructure.FilesDb.Models.ScrapedTag;
using ScrapedTagDomainId = AStar.Dev.Infrastructure.FilesDb.Models.ScrapedTagId;
using ScrapedTagDto = AStar.Dev.Wallpaper.Scrapper.DTOs.ScrapedTag;

namespace AStar.Dev.Wallpaper.Scrapper.DTOs;

public static class ScrapedTagExtensions
{
    public static ScrapedTagDomain ToDomain(this ScrapedTagDto dto, TimeProvider timeProvider)
        => new()
        {
            Id = new ScrapedTagDomainId(dto.Id.Value),
            Value = dto.Value,
            Category = dto.Category,
            IncludeInSearch = dto.IncludeInSearch,
            CreatedAt = dto.CreatedAt,
            UpdatedAt = timeProvider.GetUtcNow()
        };

    public static ScrapedTagDto ToDto(this ScrapedTagDomain domain)
        => new()
        {
            Id = new ScrapedTagId(domain.Id.Value),
            Value = domain.Value,
            Category = domain.Category,
            IncludeInSearch = domain.IncludeInSearch,
            CreatedAt = domain.CreatedAt,
            UpdatedAt = domain.UpdatedAt
        };

    public static List<ScrapedTagDomain> ToDomain(this List<ScrapedTagDto> dtos, TimeProvider timeProvider)
        => [.. dtos.Select(dto => dto.ToDomain(timeProvider))];

    public static List<ScrapedTagDto> ToDtos(this List<ScrapedTagDomain> domains)
        => [.. domains.Select(domain => domain.ToDto())];
}
