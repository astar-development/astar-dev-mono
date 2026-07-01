namespace AStar.Dev.Wallpaper.Scrapper.DTOs;

public sealed record ScrapeConfigurationDto
{
    public ConnectionStringsDto ConnectionStrings { get; init; } = new();
    public UserConfigurationDto UserConfiguration { get; init; } = new();
    public SearchConfigurationDto SearchConfiguration { get; init; } = new();
    public ScrapeDirectoriesDto ScrapeDirectories { get; init; } = new();
}
