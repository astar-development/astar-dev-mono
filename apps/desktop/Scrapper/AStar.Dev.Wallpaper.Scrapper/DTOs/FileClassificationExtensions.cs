using FileClassificationDomain = AStar.Dev.Infrastructure.AppDb.Entities.FileClassificationCategoryEntity;
using FileClassificationDto = AStar.Dev.Wallpaper.Scrapper.DTOs.FileClassification;

namespace AStar.Dev.Wallpaper.Scrapper.DTOs;

// TODO(#697): FileClassificationCategoryEntity has no Keywords navigation (keywords now live in a
// separate FileClassificationKeywordEntity table keyed by CategoryId), so keywords aren't round-tripped
// here yet. Revisit once FileClassificationService is rewritten against the category hierarchy.
public static class FileClassificationExtensions
{
    public static List<FileClassificationDomain> ToDomain(this List<FileClassificationDto> fileClassificationDtos)
        => [.. fileClassificationDtos.Select(dto => new FileClassificationDomain
        {
            Id = dto.Id,
            Name = dto.Name,
            Level = dto.Level,
            ParentId = dto.ParentId,
            IsFamous = dto.IsFamous,
            IncludeInSearch = dto.IncludeInSearch
        })];

    public static List<FileClassificationDto> ToDtos(this List<FileClassificationDomain> fileClassificationDomains)
        => [.. fileClassificationDomains.Select(domain => new FileClassificationDto
        {
            Id = domain.Id,
            Name = domain.Name,
            Level = domain.Level,
            ParentId = domain.ParentId,
            IsFamous = domain.IsFamous,
            IncludeInSearch = domain.IncludeInSearch,
            Keywords = []
        })];
}
