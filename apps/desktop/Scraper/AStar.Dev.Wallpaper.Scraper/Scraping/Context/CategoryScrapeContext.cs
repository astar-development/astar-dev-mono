using AStar.Dev.Infrastructure.AppDb.Entities;
using Microsoft.Playwright;
using AStar.Dev.Wallpaper.Scraper.Scraping.Actions;

namespace AStar.Dev.Wallpaper.Scraper.Scraping.Context;

/// <summary>
///     Carries the scrape state threaded through every stage of a single category's scrape: the shared Playwright
///     page, the progress reporter, the run-level configuration snapshot, and the category being scraped.
/// </summary>
/// <param name="Page">The Playwright page every navigation in the scrape shares.</param>
/// <param name="Progress">Receives human-readable progress messages as the scrape advances.</param>
/// <param name="ScrapeContext">The run-level configuration snapshot read once before any navigation began.</param>
/// <param name="Category">The search category currently being scraped.</param>
public sealed record CategoryScrapeContext(IPage Page, IProgress<string> Progress, ScrapeContext ScrapeContext, ScrapeCategory Category, IReadOnlyList<FileClassificationCategoryEntity> FileClassifications);
