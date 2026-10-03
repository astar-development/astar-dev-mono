using AStarDev.ControlDb.ScrapeConfiguration;

namespace AStarDev.ControlDb.TestsIntegration.TestDataFactories;

internal static class ScrapeConfigurationEntityFactory
{
    public static ScrapeConfigurationEntity CreateScrapeConfigurationEntity(Guid? id = null, string rootDirectory = "root-save-directory")
    {
        var scrapeConfigurationId = new ScrapeConfigurationId(id ?? Guid.Empty);
        var userConfigurationId = new UserConfigurationId(Guid.Empty);
        var searchConfigurationId = new SearchConfigurationId(Guid.Empty);
        var scrapeDirectoriesId = new ScrapeDirectoriesId(Guid.Empty);
        var scrapeConfiguration = new ScrapeConfigurationEntity(scrapeConfigurationId)
        {
            UserConfiguration = new UserConfigurationEntity(userConfigurationId, scrapeConfigurationId, "user@example.com", "username", "apiKey"),
            SearchConfiguration = new SearchConfigurationEntity(searchConfigurationId, scrapeConfigurationId, "search-config", 10, []),
            ScrapeDirectories = new ScrapeDirectoriesEntity(scrapeDirectoriesId, scrapeConfigurationId, rootDirectory, "root-directory-famous", "sub-directory-name"),
            BaseUrl = new Uri("https://example.com/scrape"),
            ApiKey = "scrape-api-key",
            SearchString = "search-string",
            TopWallpapers = "top-wallpapers",
            HotWallpapers = "hot-wallpapers",
            SearchStringPrefix = "prefix-",
            SearchStringSuffix = "-suffix",
            Subscriptions = "subscriptions",
            ImagePauseInSeconds = 5,
            StartingPageNumber = 1,
            TotalPages = 20,
            SubscriptionsStartingPageNumber = 2,
            SubscriptionsTotalPages = 30,
            TopWallpapersStartingPageNumber = 3,
            TopWallpapersTotalPages = 40,
            HotWallpapersStartingPageNumber = 4,
            HotWallpapersTotalPages = 50,
            LoginUrl = "https://example.com/scrape/login",
            UseHeadless = true,
            SlowMotionDelay = 250f
        };

        return scrapeConfiguration;
    }
}
