using AStar.Dev.Wallpaper.Scrapper.Repositories;
using System.IO.Abstractions;

namespace AStar.Dev.Wallpaper.Scrapper.Services;

public sealed class DatabaseResetService(IDatabaseResetRepository repository, IFileSystem fileSystem) : IDatabaseResetService
{
    public async Task ResetAsync(CancellationToken cancellationToken = default)
    {
        await repository.ResetSearchCategoriesAsync(cancellationToken).ConfigureAwait(false);
        await repository.DeleteAllFilesAsync(cancellationToken).ConfigureAwait(false);
    }

    public async Task DeleteSaveDirectoryAsync(CancellationToken cancellationToken = default)
    {
        var path = await repository.GetBaseSaveDirectoryAsync(cancellationToken).ConfigureAwait(false);

        if (string.IsNullOrWhiteSpace(path))
            return;

        if (fileSystem.Directory.Exists(path))
            fileSystem.Directory.Delete(path, recursive: true);
    }
}
