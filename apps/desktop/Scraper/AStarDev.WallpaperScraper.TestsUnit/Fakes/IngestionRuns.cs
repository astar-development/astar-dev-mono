using AStarDev.FunctionalParadigm;
using AStarDev.WallpaperScraper.ScrapeConfiguration;
using AStarDev.WallpaperScraper.Scraping;
using AStarDev.WallpaperScraper.WallpaperIngestion;

namespace AStarDev.WallpaperScraper.TestsUnit.Fakes;

/// <summary>Builds the <see cref="IngestionRun"/> a test ingests under, around the context, progress and cancellation the test cares about.</summary>
internal static class IngestionRuns
{
    public static IngestionRun Create(WallpaperIngestionContext context, IProgress<string> progress, CancellationToken cancellationToken = default)
        => new(
            new PageScrapeRequest(new ScrapeLabel("label", Option.None<string>()), Option.None<SearchCategoryProgress>(), new PageHooks(_ => { }, page => new Uri($"https://example.test/page/{page}")), new ScrapeTarget(new WallhavenConnection("api-key", new Uri("https://example.test")), [])),
            context,
            progress,
            cancellationToken);
}
