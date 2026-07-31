using AStar.Dev.Infrastructure.AppDb.Entities;

namespace AStar.Dev.Wallpaper.Scrapper.Services;

public static class PageClassificationDataFactory
{
    public static PageClassificationData Create(
        IReadOnlyList<(FileClassificationCategoryEntity Category, IReadOnlyList<string> Keywords)> searchableClassifications,
        FileClassificationCategoryEntity? categoryClassification,
        IReadOnlyList<ScrapedTagEntity> includedTags)
        => new(searchableClassifications, categoryClassification, includedTags);
}
