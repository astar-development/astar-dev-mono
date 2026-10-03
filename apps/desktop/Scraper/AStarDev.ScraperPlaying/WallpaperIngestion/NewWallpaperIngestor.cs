using AStarDev.FunctionalParadigm;
using AStarDev.ScraperPlaying.Scraping;
using AStarDev.ScraperPlaying.Scraping.WallhavenResponses.SearchResponse;
using Tag = AStarDev.ScraperPlaying.Scraping.WallhavenResponses.DetailResponse.Tag;

namespace AStarDev.ScraperPlaying.WallpaperIngestion;

/// <inheritdoc/>
public sealed class NewWallpaperIngestor(ITagFetcher tagFetcher, WallpaperFiler wallpaperFiler, IgnoredWallpaperRecorder ignoredWallpaperRecorder) : INewWallpaperIngestor
{
    /// <inheritdoc/>
    public async Task<Option<IReadOnlyList<Tag>>> FetchTagsAsync(Data wallpaper, IngestionRun run)
    {
        run.Progress.Report($"No existing data found for wallpaper {wallpaper.Id}.");

        return (await tagFetcher.FetchTagsAsync(wallpaper.Id, run))
            .Match(
                tags => Option.Some(tags),
                exception =>
                {
                    run.Progress.Report($"Failed to fetch tags for wallpaper {wallpaper.Id}: {exception.Message}");

                    return Option.None<IReadOnlyList<Tag>>();
                });
    }

    /// <inheritdoc/>
    public async Task<IngestOutcome> IngestAsync(WallpaperCandidate candidate, IReadOnlyList<Tag> tags, IngestionRun run)
    {
        if (tags.Any(tag => tag.IgnoreImage))
        {
            run.Progress.Report($"Ignoring wallpaper {candidate.Wallpaper.Id}: it has a tag flagged to ignore images.");
            ignoredWallpaperRecorder.Remember(candidate.Wallpaper.Id, run.Progress);

            return IngestOutcome.Complete;
        }

        return (await Try.RunAsync(() => wallpaperFiler.FileAsync(candidate, tags, run)))
            .Match(
                outcome => outcome,
                exception =>
                {
                    run.Progress.Report($"Failed to process image for wallpaper {candidate.Wallpaper.Id}: {exception.Message}");

                    return IngestOutcome.Incomplete;
                });
    }
}
