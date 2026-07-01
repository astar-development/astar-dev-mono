using AStar.Dev.Infrastructure.FilesDb.Models;

namespace AStar.Dev.Wallpaper.Scrapper.Services;

public static class PageClassificationDataFactory
{
    public static PageClassificationData Create(
        IReadOnlyList<FileClassification> searchableClassifications,
        FileClassification? categoryClassification,
        IReadOnlyList<ScrapedTag> includedTags)
        => new(searchableClassifications, categoryClassification, includedTags);
}
