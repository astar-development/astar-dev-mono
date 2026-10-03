using AStarDev.ControlDb;
using AStarDev.ControlDb.FileDetail;
using AStarDev.FunctionalParadigm;

namespace AStarDev.ScraperPlaying.WallpaperIngestion;

/// <summary>Remembers ignored wallpapers so later scrapes skip them before fetching their tags.</summary>
/// <param name="ignoredWallpapers">The store of ignored wallpaper handles.</param>
public sealed class IgnoredWallpaperRecorder(IIgnoredWallpapers ignoredWallpapers)
{
    /// <summary>Remembers the ignored wallpaper. A failure only costs those later fetches, so it is reported and the wallpaper is still ignored.</summary>
    /// <param name="wallpaperId">The id of the ignored wallpaper.</param>
    /// <param name="progress">Receives the failure message.</param>
    public void Remember(string wallpaperId, IProgress<string> progress)
        => _ = ignoredWallpapers.Record(FileHandle.Create(wallpaperId))
            .Match(
                unit => unit,
                exception =>
                {
                    progress.Report($"Failed to remember that wallpaper {wallpaperId} is ignored: {exception.Message}");

                    return Unit.Instance;
                });
}
