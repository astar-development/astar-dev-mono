using System.Diagnostics.CodeAnalysis;

namespace AStarDev.WallpaperScraper.Scraping;

/// <summary>A page of a search could not be fetched. Marks the failure as one that ends only that search, as opposed to a failure that leaves the scrape's unit of work in an unknown state.</summary>
/// <param name="failure">What went wrong fetching the page.</param>
[SuppressMessage("Design", "CA1032:Implement standard exception constructors", Justification = "Only ever created from the failure it wraps, so it has no message-only or parameterless form.")]
public sealed class PageFetchException(Exception failure) : Exception(failure.Message, failure)
{
    /// <summary>Gets what went wrong fetching the page.</summary>
    public Exception Failure { get; } = failure;
}
