using Microsoft.EntityFrameworkCore;

namespace AStar.Dev.Infrastructure.AppDb.Entities;

/// <summary>Provides methods for querying the scrape configuration.</summary>
public static class ScrapeConfigurationQueries
{
    /// <summary>Retrieves the most recent scrape configuration, including its connection strings, user configuration, search configuration with categories, and scrape directories.</summary>
    public static ScrapeConfigurationEntity GetScrapeConfigurations(this DbSet<ScrapeConfigurationEntity> configurations)
        => configurations
            .Include(e => e.ConnectionStrings)
            .Include(e => e.UserConfiguration)
            .Include(e => e.SearchConfiguration).ThenInclude(s => s.SearchCategories)
            .Include(e => e.ScrapeDirectories)
            .OrderByDescending(e => e.Id)
            .First();
}
