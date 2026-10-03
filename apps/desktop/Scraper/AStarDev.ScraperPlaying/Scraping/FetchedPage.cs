using AStarDev.ScraperPlaying.Scraping.WallhavenResponses.SearchResponse;

namespace AStarDev.ScraperPlaying.Scraping;

/// <summary>A page of search results together with the page number it was fetched as.</summary>
/// <param name="Number">The page number.</param>
/// <param name="Response">The search response for that page.</param>
public readonly record struct FetchedPage(int Number, SearchResponse Response);
