namespace AStar.Dev.Wallpaper.Scrapper.DTOs;

public sealed record SearchCategoryDto
{
    public string Id { get; init; } = string.Empty;
    public string Name { get; init; } = string.Empty;
    public int LastKnownImageCount { get; init; }
    public int LastPageVisited { get; init; }
    public int TotalPages { get; init; }
    public bool IncludeInSearch { get; init; } = true;
}
