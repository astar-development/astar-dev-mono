using Microsoft.EntityFrameworkCore;

namespace AStarDev.ControlDb.ScrapeConfiguration;

/// <summary>
/// Eagerly loads a <see cref="ScrapeConfigurationEntity"/> aggregate together with its required sub-entities
/// (<see cref="ScrapeConfigurationEntity.UserConfiguration"/>, <see cref="ScrapeConfigurationEntity.SearchConfiguration"/>,
/// its <see cref="SearchConfigurationEntity.SearchCategories"/> and <see cref="SearchConfigurationEntity.PersonCategories"/>, and <see cref="ScrapeConfigurationEntity.ScrapeDirectories"/>).
/// The load is a split query, so the two sibling collections are not joined into one cartesian product, and it is in <see cref="ScrapeConfigurationOrdering.InFirstOrder"/> so the first row is deterministic.
/// </summary>
public sealed class ScrapeConfigurationQuery : IQuery<ScrapeConfigurationEntity>
{
    /// <inheritdoc/>
    public IQueryable<ScrapeConfigurationEntity> Apply(IQueryable<ScrapeConfigurationEntity> query) =>
        query
            .InFirstOrder()
            .Include(scrapeConfiguration => scrapeConfiguration.UserConfiguration)
            .Include(scrapeConfiguration => scrapeConfiguration.SearchConfiguration)
                .ThenInclude(searchConfiguration => searchConfiguration.SearchCategories)
            .Include(scrapeConfiguration => scrapeConfiguration.SearchConfiguration)
                .ThenInclude(searchConfiguration => searchConfiguration.PersonCategories)
            .Include(scrapeConfiguration => scrapeConfiguration.ScrapeDirectories)
            .AsSplitQuery();
}
