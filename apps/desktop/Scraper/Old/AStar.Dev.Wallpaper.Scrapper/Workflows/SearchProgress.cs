using AStar.Dev.Wallpaper.Scrapper.Models;

namespace AStar.Dev.Wallpaper.Scrapper.Workflows;

/// <summary>The mutable-in-name-only loop state threaded through a <see cref="SearchWorkflow" /> run.</summary>
/// <param name="SearchConfiguration">The current search configuration.</param>
/// <param name="ScrapeDirectories">The current scrape directories.</param>
public record SearchProgress(SearchConfiguration SearchConfiguration, ScrapeDirectories ScrapeDirectories);
