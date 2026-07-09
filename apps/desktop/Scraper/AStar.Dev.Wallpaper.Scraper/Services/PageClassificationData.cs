using AStar.Dev.Infrastructure.AppDb.Entities;

namespace AStar.Dev.Wallpaper.Scraper.Services;

public record PageClassificationData(IReadOnlyList<(FileClassificationCategoryEntity Category, IReadOnlyList<string> Keywords)> SearchableClassifications, FileClassificationCategoryEntity? CategoryClassification, IReadOnlyList<FileClassificationCategoryEntity> IncludedTags);
