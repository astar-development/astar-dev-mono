namespace AStar.Dev.Wallpaper.Scrapper.Models;

public sealed class Configuration
{
    public Logging Logging { get; set; } = new();

    public ScrapeConfiguration ScrapeConfiguration { get; set; } = default!;
}
