namespace AStar.Dev.ScrapeCategoryImportExportTesting;

public record CategoryNodeRecord(string Name, int Level, bool IsFamous, bool IsInternet, string? ParentName, DateTimeOffset CreatedAt, DateTimeOffset? UpdatedAt = null);
