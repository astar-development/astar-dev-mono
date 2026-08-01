namespace AStar.Dev.Wallpaper.Scrapper.Repositories;

public interface IDatabaseResetRepository
{
    Task ResetSearchCategoriesAsync(CancellationToken cancellationToken = default);
    Task DeleteAllFilesAsync(CancellationToken cancellationToken = default);
    Task<string?> GetBaseSaveDirectoryAsync(CancellationToken cancellationToken = default);
}
