using FileClassificationDomain = AStar.Dev.Infrastructure.AppDb.Entities.FileClassificationCategoryEntity;
using FileClassificationDto = AStar.Dev.Wallpaper.Scrapper.DTOs.FileClassification;
using FileClassificationKeywordDomain = AStar.Dev.Infrastructure.AppDb.Entities.FileClassificationKeywordEntity;

namespace AStar.Dev.Wallpaper.Scrapper.DTOs;

public static class FileClassificationExtensions
{
    public static (List<FileClassificationDomain> Categories, List<FileClassificationKeywordDomain> Keywords) ToDomain(this List<FileClassificationDto> fileClassificationDtos)
    {
        List<FileClassificationDomain> categories = [];
        List<FileClassificationKeywordDomain> keywords = [];

        foreach (var dto in fileClassificationDtos)
        {
            categories.Add(new FileClassificationDomain
            {
                Id = dto.Id,
                Name = dto.Name,
                Level = dto.Level,
                ParentId = dto.ParentId,
                IsFamous = dto.IsFamous,
                IncludeInSearch = dto.IncludeInSearch
            });

            keywords.AddRange(dto.Keywords.Select(keywordDto => new FileClassificationKeywordDomain
            {
                Id = keywordDto.Id,
                Keyword = keywordDto.Keyword,
                CategoryId = dto.Id
            }));
        }

        return (categories, keywords);
    }

    public static List<FileClassificationDto> ToDtos(this (List<FileClassificationDomain> Categories, List<FileClassificationKeywordDomain> Keywords) fileClassifications)
        => [.. fileClassifications.Categories.Select(category => new FileClassificationDto
        {
            Id = category.Id,
            Name = category.Name,
            Level = category.Level,
            ParentId = category.ParentId,
            IsFamous = category.IsFamous,
            IncludeInSearch = category.IncludeInSearch,
            Keywords = [.. fileClassifications.Keywords
                .Where(keyword => keyword.CategoryId == category.Id)
                .Select(keyword => new FileClassificationKeyword { Id = keyword.Id, Keyword = keyword.Keyword })]
        })];
}
