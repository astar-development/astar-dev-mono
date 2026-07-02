using AStar.Dev.Infrastructure.FilesDb.Models;
using Microsoft.EntityFrameworkCore;

namespace AStar.Dev.Infrastructure.FilesDb.Data;

/// <summary>
///     Provides methods for querying file classifications.
/// </summary>
public static class ClassificationQueries
{
    /// <summary>
    ///   Retrieves the scrape configuration from the database, including related entities such as connection strings, user configuration, search configuration with its categories, and scrape directories. This method ensures that all necessary configuration details are loaded in a single query, facilitating efficient access to the scraper's settings.
    /// </summary>
    /// <param name="configurations">The set of scrape configurations.</param>
    /// <returns>The first scrape configuration entity.</returns>
    public static ScrapeConfigurationEntity GetScrapeConfigurations(this DbSet<ScrapeConfigurationEntity> configurations)
        => configurations
                .Include(e => e.ConnectionStrings)
                .Include(e => e.UserConfiguration)
                .Include(e => e.SearchConfiguration).ThenInclude(s => s.SearchCategories)
                .Include(e => e.ScrapeDirectories)
                .OrderByDescending(s => s.Id)
                .First();
}