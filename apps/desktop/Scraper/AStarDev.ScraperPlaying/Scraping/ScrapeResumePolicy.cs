using AStarDev.FunctionalParadigm;
using AStarDev.ScraperPlaying.ScrapeConfiguration;
using AStarDev.ScraperPlaying.Scraping.WallhavenResponses.SearchResponse;

namespace AStarDev.ScraperPlaying.Scraping;

/// <summary>Decides where a scrape resumes, when a category is unchanged since the previous scrape and when paging stops.</summary>
/// <param name="limits">How much a single scrape is allowed to fetch.</param>
public sealed class ScrapeResumePolicy(ScrapeLimits limits)
{
    /// <summary>The page to start from: the page after the last one visited when the previous scrape stopped part-way, otherwise the first page.</summary>
    /// <param name="previousProgress">The progress recorded by the previous scrape, if any.</param>
    public int ResumePage(Option<SearchCategoryProgress> previousProgress)
        => previousProgress.Match(
            previous => previous.LastPageVisited > 0 && !IsComplete(previous) ? previous.LastPageVisited + 1 : 1,
            () => 1);

    /// <summary>Whether the page just fetched belongs to the same category as the previous scrape, so resuming part-way is valid.</summary>
    /// <param name="previousProgress">The progress recorded by the previous scrape, if any.</param>
    /// <param name="meta">The paging metadata of the page just fetched.</param>
    public static bool IsSameCategoryAsPreviousScrape(Option<SearchCategoryProgress> previousProgress, Meta meta)
        => previousProgress.Match(
            previous => previous.LastKnownImageCount == meta.Total && previous.TotalPages == meta.LastPage,
            () => false);

    /// <summary>Whether the category is unchanged and was completely scraped last time, so it can be skipped.</summary>
    /// <param name="previousProgress">The progress recorded by the previous scrape, if any.</param>
    /// <param name="meta">The paging metadata of the page just fetched.</param>
    public bool IsUnchangedSincePreviousScrape(Option<SearchCategoryProgress> previousProgress, Meta meta)
        => previousProgress.Match(
            previous => IsSameCategoryAsPreviousScrape(previousProgress, meta) && IsComplete(previous),
            () => false);

    /// <summary>Whether no further page should be visited after <paramref name="pageNumber"/>.</summary>
    /// <param name="pageNumber">The page just visited.</param>
    /// <param name="meta">The paging metadata of that page.</param>
    public bool IsLastPageToVisit(int pageNumber, Meta meta)
        => pageNumber >= meta.LastPage || pageNumber >= limits.MaximumPagesPerSearch;

    private bool IsComplete(SearchCategoryProgress previous)
        => previous.LastPageVisited >= Math.Min(previous.TotalPages, limits.MaximumPagesPerSearch);
}
