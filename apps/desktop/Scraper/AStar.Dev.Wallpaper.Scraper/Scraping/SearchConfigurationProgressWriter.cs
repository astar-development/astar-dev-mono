using AStar.Dev.Infrastructure.AppDb;
using AStar.Dev.Infrastructure.AppDb.Entities;
using Microsoft.EntityFrameworkCore;

namespace AStar.Dev.Wallpaper.Scraper.Scraping;

/// <inheritdoc cref="ISearchConfigurationProgressWriter" />
public sealed class SearchConfigurationProgressWriter(IDbContextFactory<AppDbContext> dbContextFactory) : ISearchConfigurationProgressWriter
{
    /// <inheritdoc />
    public Task WriteTopWallpapersProgressAsync(int startingPageNumber, int totalPages, CancellationToken cancellationToken) =>
        WriteAsync(configuration =>
        {
            configuration.TopWallpapersStartingPageNumber = startingPageNumber;
            configuration.TopWallpapersTotalPages = totalPages;
        }, cancellationToken);

    /// <inheritdoc />
    public Task WriteSubscriptionsProgressAsync(int startingPageNumber, int totalPages, CancellationToken cancellationToken) =>
        WriteAsync(configuration =>
        {
            configuration.SubscriptionsStartingPageNumber = startingPageNumber;
            configuration.SubscriptionsTotalPages = totalPages;
        }, cancellationToken);

    private async Task WriteAsync(Action<SearchConfigurationEntity> applyProgress, CancellationToken cancellationToken)
    {
        await using var context = await dbContextFactory.CreateDbContextAsync(cancellationToken);
        var configuration = await context.SearchConfigurations.OrderByDescending(configuration => configuration.UpdatedAt).FirstAsync(cancellationToken);

        applyProgress(configuration);
        configuration.UpdatedAt = DateTime.UtcNow;

        await context.SaveChangesAsync(cancellationToken);
    }
}
