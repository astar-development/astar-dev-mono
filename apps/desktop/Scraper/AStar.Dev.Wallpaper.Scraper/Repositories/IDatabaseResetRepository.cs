using AStar.Dev.FunctionalParadigm;
using AStar.Dev.Wallpaper.Scraper.Models;

namespace AStar.Dev.Wallpaper.Scraper.Repositories;

public interface IDatabaseResetRepository
{
    Task<Result<Unit, ScrapeError>> ResetSearchCategoriesAsync(CancellationToken cancellationToken = default);
    Task<Result<Unit, ScrapeError>> DeleteAllFilesAsync(CancellationToken cancellationToken = default);
    Task<Result<Option<string>, ScrapeError>> GetBaseSaveDirectoryAsync(CancellationToken cancellationToken = default);
}
