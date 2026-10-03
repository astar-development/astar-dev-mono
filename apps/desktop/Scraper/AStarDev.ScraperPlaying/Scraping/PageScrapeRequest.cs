using AStarDev.FunctionalParadigm;
using AStarDev.ScraperPlaying.ScrapeConfiguration;

namespace AStarDev.ScraperPlaying.Scraping;

/// <summary>Describes one paged scrape of wallpapers: what is being scraped, where it left off last time, how to build and report each page and where to fetch from.</summary>
/// <param name="Label">What is being scraped.</param>
/// <param name="PreviousProgress">The progress recorded by the previous scrape of the category, or <see cref="Option{T}.None"/> when there is none or it is the "Top Wallpapers" scrape, which is never skipped. When the previous scrape stopped part-way and the category is unchanged, paging resumes after the last page visited.</param>
/// <param name="Hooks">How to build and report each page.</param>
/// <param name="Target">Where to fetch pages from and how saved files are named.</param>
public sealed record PageScrapeRequest(
    ScrapeLabel Label,
    Option<SearchCategoryProgress> PreviousProgress,
    PageHooks Hooks,
    ScrapeTarget Target);

/// <summary>Identifies a scrape for logging and saving.</summary>
/// <param name="LogLabel">A label used for logging purposes.</param>
/// <param name="CategoryName">The search category name when a category search is being performed, or <see cref="Option{T}.None"/> when fetching the "Top Wallpapers" scrape.</param>
public sealed record ScrapeLabel(string LogLabel, Option<string> CategoryName);

/// <summary>The page-level behaviour of a scrape.</summary>
/// <param name="OnPageCompleted">Called after each page is ingested, before it is saved, with the progress so far, so an interrupted scrape can resume from the last page visited.</param>
/// <param name="PageUrlFactory">A function that generates page URLs based on the page number.</param>
public sealed record PageHooks(Action<SearchCategoryProgress> OnPageCompleted, Func<int, Uri> PageUrlFactory);

/// <summary>Where a scrape fetches from and how saved files are named.</summary>
/// <param name="Connection">The Wallhaven API credentials and target host to fetch pages from.</param>
/// <param name="PersonCategories">The tag category names whose tags are person names when naming saved files.</param>
public sealed record ScrapeTarget(WallhavenConnection Connection, IReadOnlyList<string> PersonCategories);
