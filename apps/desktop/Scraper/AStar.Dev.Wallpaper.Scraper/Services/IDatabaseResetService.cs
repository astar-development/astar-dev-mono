namespace AStar.Dev.Wallpaper.Scraper.Services;

public interface IDatabaseResetService
{
    Task ResetAsync(CancellationToken cancellationToken = default);
    Task DeleteSaveDirectoryAsync(CancellationToken cancellationToken = default);
}
