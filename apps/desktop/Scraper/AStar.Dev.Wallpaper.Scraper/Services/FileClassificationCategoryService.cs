using AStar.Dev.FunctionalParadigm;
using AStar.Dev.Wallpaper.Scraper.Models;
using AStar.Dev.Wallpaper.Scraper.Repositories;
using FileClassificationCategoryDomain = AStar.Dev.Infrastructure.AppDb.Entities.FileClassificationCategoryEntity;

namespace AStar.Dev.Wallpaper.Scraper.Services;

public sealed class FileClassificationCategoryService(IFileClassificationCategoriesRepository repository) : IFileClassificationCategoryService
{
    public Task<Result<List<FileClassificationCategoryDomain>, ScrapeError>> ExportScrapedTagsAsync(CancellationToken cancellationToken)
        => repository.GetAllAsync(cancellationToken);

    public Task<Result<int, ScrapeError>> ImportScrapedTagsAsync(IReadOnlyList<FileClassificationCategoryDomain> tags, CancellationToken cancellationToken)
        => repository.UpsertAsync(tags, cancellationToken).BindAsync(_ => Task.FromResult<Result<int, ScrapeError>>(tags.Count));
}
