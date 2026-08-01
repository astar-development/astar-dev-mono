namespace AStar.Dev.Wallpaper.Scraper.Scraping.Actions;

/// <summary>
///     Marks the <see cref="IScrapeAction" /> implementation that scrapes the Subscriptions listing, giving it a
///     distinct DI registration from the other <see cref="IScrapeAction" /> implementations.
/// </summary>
public interface ISubscriptionsScrapeAction : IScrapeAction;
