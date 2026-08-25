using AStar.Dev.FunctionalParadigm;
using AStarDev.ControlDb;
using Microsoft.EntityFrameworkCore;

namespace AStarDev.WallpaperScraper.Scrapers;

/// <summary>
/// Represents a repository for scrape configuration settings repository.
/// </summary>
/// <param name="dbContextFactory">The factory for creating instances of the ControlDbContext.</param>
public sealed class ScrapeConfigurationRepository(IDbContextFactory<ControlDbContext> dbContextFactory) : IScrapeConfigurationRepository
{
    /// <inheritdoc/>
    public async Task<Exceptional<ScrapeConfiguration>> GetScrapeConfigurationAsync()
        => await Try.RunAsync(async () =>
            {
                using var dbContext = dbContextFactory.CreateDbContext();
                var config = await dbContext.ScrapeConfigurations.FirstAsync();

                return new ScrapeConfiguration(
                    new Uri(config.SearchConfiguration.BaseUrl + config.SearchConfiguration.SearchStringPrefix),
                    new Uri(config.SearchConfiguration.BaseUrl + config.SearchConfiguration.TopWallpapers),
                    new Uri(config.SearchConfiguration.BaseUrl + config.SearchConfiguration.Subscriptions));
            });
}
