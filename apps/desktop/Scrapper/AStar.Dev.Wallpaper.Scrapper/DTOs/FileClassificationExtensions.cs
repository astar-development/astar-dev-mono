using FileClassificationDto = AStar.Dev.Wallpaper.Scrapper.DTOs.FileClassification;
using FileClassificationDomain = AStar.Dev.Infrastructure.FilesDb.Models.FileClassification;
using FileNamePartDomain = AStar.Dev.Infrastructure.FilesDb.Models.FileNamePart;

namespace AStar.Dev.Wallpaper.Scrapper.DTOs;

public static class FileClassificationExtensions    
{
    public static List<FileClassificationDomain> ToDomain(this List<FileClassificationDto> fileClassificationDtos)
        => [.. fileClassificationDtos.Select(dto => new FileClassificationDomain
        {
            Id = dto.Id,
            Name = dto.Name,
            Celebrity = dto.Celebrity,
            IncludeInSearch = dto.IncludeInSearch,
            CreatedAt = dto.CreatedAt,
            UpdatedAt = dto.UpdatedAt,
            FileNameParts = [.. dto.FileNameParts.Select(partDto => new FileNamePartDomain
            {
                Id = partDto.Id,
                Text = partDto.Text,
                CreatedAt = partDto.CreatedAt,
                UpdatedAt = partDto.UpdatedAt
            })]
        })];

    public static List<FileClassificationDto> ToDtos(this List<FileClassificationDomain> fileClassificationDomains)
        => [.. fileClassificationDomains.Select(domain => new FileClassificationDto
        {
            Id = domain.Id,
            Name = domain.Name,
            Celebrity = domain.Celebrity,
            IncludeInSearch = domain.IncludeInSearch,
            CreatedAt = domain.CreatedAt,
            UpdatedAt = domain.UpdatedAt,
            FileNameParts = [.. domain.FileNameParts.Select(partDomain => new DTOs.FileNamePart
            {
                Id = partDomain.Id,
                Text = partDomain.Text,
                CreatedAt = partDomain.CreatedAt,
                UpdatedAt = partDomain.UpdatedAt
            })]
        })];
}
