using AStarDev.FunctionalParadigm;
using AStarDev.ScraperPlaying.ScrapeConfiguration;

namespace AStarDev.ScraperPlaying.UI;

/// <summary>What the configuration editor needs to open and to switch between configurations.</summary>
/// <param name="Initial">The configuration the editor opens on.</param>
/// <param name="Summaries">Every scrape configuration the user can switch to.</param>
/// <param name="LoadAsync">Loads the configuration the user switches to; none, with the reason already reported, if it could not be loaded.</param>
public sealed record ConfigurationEditorSession(
    LoadedConfiguration Initial,
    IReadOnlyList<ScrapeConfigurationSummary> Summaries,
    Func<ScrapeConfigurationSummary, Task<Option<LoadedConfiguration>>> LoadAsync);
