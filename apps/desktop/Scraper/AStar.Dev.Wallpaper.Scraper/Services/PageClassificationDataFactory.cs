using AStar.Dev.Infrastructure.AppDb.Entities;

namespace AStar.Dev.Wallpaper.Scraper.Services;

public static class PageClassificationDataFactory
{
    public static PageClassificationData Create(
        IReadOnlyList<(FileClassificationCategoryEntity Category, IReadOnlyList<string> Keywords)> searchableClassifications,
        FileClassificationCategoryEntity? categoryClassification,
        IReadOnlyList<FileClassificationCategoryEntity> includedTags)
        => new(searchableClassifications, categoryClassification, includedTags);
}
