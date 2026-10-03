namespace AStarDev.ControlDb.ScrapeConfiguration;

/// <summary>The one ordering every "first scrape configuration" lookup uses, so they all pick the same row.</summary>
public static class ScrapeConfigurationOrdering
{
    /// <summary>Orders configurations by id. Ids are version 7 GUIDs, so the oldest configuration comes first.</summary>
    /// <param name="query">The configurations to order.</param>
    /// <returns>The configurations, oldest first.</returns>
    public static IQueryable<ScrapeConfigurationEntity> InFirstOrder(this IQueryable<ScrapeConfigurationEntity> query)
        => query.OrderBy(scrapeConfiguration => scrapeConfiguration.Id);
}
