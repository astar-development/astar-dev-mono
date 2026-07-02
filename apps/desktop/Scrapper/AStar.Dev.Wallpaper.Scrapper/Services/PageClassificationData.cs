using AStar.Dev.Infrastructure.FilesDb.Models;

namespace AStar.Dev.Wallpaper.Scrapper.Services;

public record PageClassificationData(IReadOnlyList<FileClassification> SearchableClassifications, FileClassification? CategoryClassification, IReadOnlyList<ScrapedTag> IncludedTags);
