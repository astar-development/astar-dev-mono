using System.IO.Abstractions;
using AStar.Dev.FunctionalParadigm;
using AStar.Dev.Wallpaper.Scraper.Models;
using AStar.Dev.Wallpaper.Scraper.Repositories;

namespace AStar.Dev.Wallpaper.Scraper.Services;

public sealed class DatabaseResetService(IDatabaseResetRepository repository, IFileSystem fileSystem) : IDatabaseResetService
{
    public Task<Result<Unit, ScrapeError>> ResetAsync(CancellationToken cancellationToken = default)
        => repository.ResetSearchCategoriesAsync(cancellationToken)
            .BindAsync(_ => repository.DeleteAllFilesAsync(cancellationToken));

    public Task<Result<Unit, ScrapeError>> DeleteSaveDirectoryAsync(CancellationToken cancellationToken = default)
        => repository.GetBaseSaveDirectoryAsync(cancellationToken)
            .BindAsync(baseSaveDirectory => Task.FromResult<Result<Unit, ScrapeError>>(DeleteIfExists(baseSaveDirectory)));

    private Unit DeleteIfExists(Option<string> baseSaveDirectory)
        => baseSaveDirectory.Match(
            path =>
            {
                if (!string.IsNullOrWhiteSpace(path) && fileSystem.Directory.Exists(path))
                    fileSystem.Directory.Delete(path, recursive: true);

                return Unit.Value;
            },
            () => Unit.Value);
}
