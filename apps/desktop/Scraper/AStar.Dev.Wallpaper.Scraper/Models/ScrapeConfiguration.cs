namespace AStar.Dev.Wallpaper.Scraper.Models;

public record ScrapeConfiguration(ConnectionStrings ConnectionStrings, UserConfiguration UserConfiguration, SearchConfiguration SearchConfiguration, ScrapeDirectories ScrapeDirectories)
{
    public ScrapeConfiguration() : this(default!, default!, default!, default!)
    {
    }
}
