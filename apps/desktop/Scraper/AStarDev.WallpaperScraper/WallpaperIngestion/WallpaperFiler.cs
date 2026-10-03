using AStarDev.ControlDb.FileDetail;
using AStarDev.FunctionalParadigm;
using AStarDev.WallpaperScraper.Scraping;
using AStarDev.WallpaperScraper.Scraping.WallhavenResponses.SearchResponse;
using Tag = AStarDev.WallpaperScraper.Scraping.WallhavenResponses.DetailResponse.Tag;

namespace AStarDev.WallpaperScraper.WallpaperIngestion;

/// <summary>Files a wallpaper that is to be kept: saves and records it, links its tags, then announces it.</summary>
/// <param name="wallpaperSaver">Downloads the image and records the file entity.</param>
/// <param name="tagLinker">Links the tags to the recorded file.</param>
/// <param name="imageDownloadNotifier">Tells listeners the wallpaper arrived.</param>
public sealed class WallpaperFiler(WallpaperSaver wallpaperSaver, ITagLinker tagLinker, IImageDownloadNotifier imageDownloadNotifier)
{
    /// <summary>Saves the wallpaper, links its tags and announces the download.</summary>
    /// <param name="candidate">The wallpaper and the extension its image is saved with.</param>
    /// <param name="tags">The wallpaper's tags, which decide where and under what name it is saved.</param>
    /// <param name="run">The run being ingested, giving the context, progress and cancellation.</param>
    /// <returns><see cref="IngestOutcome.Complete"/> when the wallpaper is saved and linked, or <see cref="IngestOutcome.Incomplete"/> when linking failed and the file was discarded; a save failure or a cancellation is thrown.</returns>
    public async Task<IngestOutcome> FileAsync(WallpaperCandidate candidate, IReadOnlyList<Tag> tags, IngestionRun run)
    {
        var request = new WallpaperFileRequest(candidate.Wallpaper, run.Context.Output.Directories.For(tags), WallpaperFileNamer.Create(candidate.Wallpaper.Id, candidate.Extension, tags), run.Context.Output.CategoryLabel);

        var saved = await wallpaperSaver.SaveAsync(request, run.Context, run.Progress, run.CancellationToken);

        return await LinkTagsAsync(candidate.Wallpaper.Id, saved, tags, run);
    }

    /// <summary>Links the tags to the recorded file, then announces the download. The file is recorded first, so if linking fails or is cancelled it is discarded: left tracked, the page save would persist an untagged file that later scrapes treat as already ingested. Listeners are only told of a wallpaper that survives linking, so a preview never shows a discarded image.</summary>
    private async Task<IngestOutcome> LinkTagsAsync(string wallpaperId, SavedWallpaper saved, IReadOnlyList<Tag> tags, IngestionRun run)
    {
        try
        {
            return (await tagLinker.LinkTagsAsync(saved.Entity.Id, tags, run.CancellationToken))
                .Match(
                    _ =>
                    {
                        imageDownloadNotifier.NotifyImageDownloaded(saved.Details with { Info = saved.Details.Info with { Count = run.Tally.RecordDownload() } });

                        return IngestOutcome.Complete;
                    },
                    exception =>
                    {
                        run.Progress.Report($"Failed to link tags for wallpaper {wallpaperId}: {exception.Message}");
                        Discard(saved.Entity, wallpaperId, run);

                        return IngestOutcome.Incomplete;
                    });
        }
        catch (OperationCanceledException)
        {
            Discard(saved.Entity, wallpaperId, run);

            throw;
        }
    }

    private static void Discard(FileEntity fileEntity, string wallpaperId, IngestionRun run)
        => _ = run.Context.FileRepository.Delete(fileEntity)
            .Match(
                unit => unit,
                exception =>
                {
                    run.Progress.Report($"Failed to discard the untagged file record for wallpaper {wallpaperId}: {exception.Message}");

                    return Unit.Instance;
                });
}
