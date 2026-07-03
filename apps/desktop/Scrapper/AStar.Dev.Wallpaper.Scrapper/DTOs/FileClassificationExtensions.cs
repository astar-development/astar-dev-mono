using FileClassificationDomain = AStar.Dev.Infrastructure.FilesDb.Models.FileClassification;
using FileClassificationDto = AStar.Dev.Wallpaper.Scrapper.DTOs.FileClassification;
using FileClassificationKeywordDomain = AStar.Dev.Infrastructure.FilesDb.Models.FileClassificationKeyword;

namespace AStar.Dev.Wallpaper.Scrapper.DTOs;

public static class FileClassificationExtensions
{
    public static List<FileClassificationDomain> ToDomain(this List<FileClassificationDto> fileClassificationDtos)
        => [.. fileClassificationDtos.Select(dto =>
        {
            var domain = new FileClassificationDomain
            {
                Id = dto.Id,
                Name = dto.Name,
                Level = dto.Level,
                ParentId = dto.ParentId,
                IsFamous = dto.IsFamous,
                IncludeInSearch = dto.IncludeInSearch,
                CreatedAt = dto.CreatedAt,
                UpdatedAt = dto.UpdatedAt
            };

            foreach (var keywordDto in dto.Keywords)
            {
                domain.Keywords.Add(new FileClassificationKeywordDomain
                {
                    Id = keywordDto.Id,
                    Keyword = keywordDto.Keyword,
                    CreatedAt = keywordDto.CreatedAt,
                    UpdatedAt = keywordDto.UpdatedAt
                });
            }

            return domain;
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
            CreatedAt = domain.CreatedAt,
            UpdatedAt = domain.UpdatedAt,
            Keywords = [.. domain.Keywords.Select(keywordDomain => new DTOs.FileClassificationKeyword
            {
                Id = keywordDomain.Id,
                Keyword = keywordDomain.Keyword,
                CreatedAt = keywordDomain.CreatedAt,
                UpdatedAt = keywordDomain.UpdatedAt
            })]
        })];
}
