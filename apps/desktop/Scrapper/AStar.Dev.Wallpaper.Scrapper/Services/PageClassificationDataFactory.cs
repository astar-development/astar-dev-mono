using AStar.Dev.Infrastructure.AppDb.Entities;

namespace AStar.Dev.Wallpaper.Scrapper.Services;

public static class PageClassificationDataFactory
{
    public static PageClassificationData Create(
        IReadOnlyList<FileClassificationCategoryEntity> searchableClassifications,
        FileClassificationCategoryEntity? categoryClassification,
        IReadOnlyList<ScrapedTagEntity> includedTags)
        => new(searchableClassifications, categoryClassification, includedTags);
}
