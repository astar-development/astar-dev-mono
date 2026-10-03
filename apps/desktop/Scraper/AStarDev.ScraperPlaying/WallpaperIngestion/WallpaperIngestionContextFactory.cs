using AStarDev.ControlDb;
using AStarDev.ControlDb.FileDetail;
using AStarDev.FunctionalParadigm;
using AStarDev.ScraperPlaying.Scraping;

namespace AStarDev.ScraperPlaying.WallpaperIngestion;

/// <inheritdoc/>
public sealed class WallpaperIngestionContextFactory(IWallhavenClientFactory clientFactory, IUnitOfWork unitOfWork, ISaveDirectoryResolver saveDirectoryResolver) : IWallpaperIngestionContextFactory
{
    /// <inheritdoc/>
    public async Task<WallpaperIngestionContext> CreateAsync(PageScrapeRequest request, CancellationToken cancellationToken)
    {
        var client = clientFactory.Create(request.Target.Connection);
        var directories = await saveDirectoryResolver.ResolveSaveDirectoriesAsync(request.Label.CategoryName, cancellationToken);
        var categoryLabel = request.Label.CategoryName.Match(name => name, () => "Top Wallpapers");

        return new WallpaperIngestionContext(new SearchOutput(directories, categoryLabel), client, unitOfWork.GetRepository<FileEntity, FileId>(), request.Target.PersonCategories);
    }
}
