namespace AStar.Dev.Wallpaper.Scrapper.DTOs;

public sealed record ScrapeDirectoriesDto
{
    public string RootDirectory { get; init; } = string.Empty;
    public string BaseSaveDirectory { get; init; } = string.Empty;
    public string BaseDirectory { get; init; } = string.Empty;
    public string BaseDirectoryFamous { get; init; } = string.Empty;
    public string SubDirectoryName { get; init; } = string.Empty;
}
