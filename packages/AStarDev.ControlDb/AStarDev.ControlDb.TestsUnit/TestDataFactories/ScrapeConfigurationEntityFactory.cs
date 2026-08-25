using AStarDev.ControlDb.ScrapeConfiguration;

namespace AStarDev.ControlDb.TestsUnit.TestDataFactories;

internal static class ScrapeConfigurationEntityFactory
{
    public static ScrapeConfigurationEntity CreateScrapeConfigurationEntity()
    {
        var scrapeConfigurationId = new ScrapeConfigurationId(Guid.Empty);
        var userConfigurationId = new UserConfigurationId(Guid.Empty);
        var searchConfigurationId = new SearchConfigurationId(Guid.Empty);
        var scrapeDirectoriesId = new ScrapeDirectoriesId(Guid.Empty);
        var scrapeConfiguration = new ScrapeConfigurationEntity(scrapeConfigurationId)
        {
            UserConfiguration = new UserConfigurationEntity(userConfigurationId, scrapeConfigurationId, "user@example.com", "username", "password", "session-cookie"),
            SearchConfiguration = new SearchConfigurationEntity(searchConfigurationId, scrapeConfigurationId, "search-config", 10, []),
            ScrapeDirectories = new ScrapeDirectoriesEntity(scrapeDirectoriesId, scrapeConfigurationId, "scrape-directory", "base-save-directory", "base-directory", "base-directory-famous", "sub-directory-name")
        };

        return scrapeConfiguration;
    }
}
