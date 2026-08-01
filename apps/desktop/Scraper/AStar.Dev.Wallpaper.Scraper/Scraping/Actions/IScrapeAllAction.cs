namespace AStar.Dev.Wallpaper.Scraper.Scraping.Actions;

/// <summary>
///     Marks the <see cref="IScrapeAction" /> implementation that runs every other scrape action in sequence,
///     giving it a distinct DI registration from the other <see cref="IScrapeAction" /> implementations.
/// </summary>
public interface IScrapeAllAction : IScrapeAction;
