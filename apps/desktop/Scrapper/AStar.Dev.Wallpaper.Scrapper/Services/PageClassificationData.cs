using AStar.Dev.Infrastructure.AppDb.Entities;

namespace AStar.Dev.Wallpaper.Scrapper.Services;

public record PageClassificationData(IReadOnlyList<FileClassificationCategoryEntity> SearchableClassifications, FileClassificationCategoryEntity? CategoryClassification, IReadOnlyList<ScrapedTagEntity> IncludedTags);
