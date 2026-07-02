namespace AStar.Dev.Wallpaper.Scrapper.Services;

public interface IDatabaseResetService
{
    Task ResetAsync(CancellationToken cancellationToken = default);
    Task DeleteSaveDirectoryAsync(CancellationToken cancellationToken = default);
}
