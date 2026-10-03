namespace AStarDev.ScraperPlaying.UI;

/// <summary>The actions the main window's menus and buttons trigger, grouped by domain so the window only has to translate control events into calls.</summary>
/// <param name="Configuration">The scrape configuration import, export and edit actions.</param>
/// <param name="Tags">The tag actions.</param>
/// <param name="Scrape">The scrape, cancel and clear downloads actions.</param>
public sealed record MainWindowActions(ConfigurationActions Configuration, TagActions Tags, ScrapeActions Scrape);
