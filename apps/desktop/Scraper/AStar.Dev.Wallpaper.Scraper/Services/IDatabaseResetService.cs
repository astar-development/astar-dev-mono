using AStar.Dev.FunctionalParadigm;
using AStar.Dev.Wallpaper.Scraper.Models;

namespace AStar.Dev.Wallpaper.Scraper.Services;

public interface IDatabaseResetService
{
    Task<Result<Unit, ScrapeError>> ResetAsync(CancellationToken cancellationToken = default);
    Task<Result<Unit, ScrapeError>> DeleteSaveDirectoryAsync(CancellationToken cancellationToken = default);
}
