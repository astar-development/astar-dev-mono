using AStarDev.ControlDb.ScrapeConfiguration;

namespace AStarDev.ScraperPlaying.TestsUnit.Fakes;

/// <summary>Builds scrape configuration entities for tests.</summary>
internal static class ScrapeConfigurationTestData
{
    public static ScrapeConfigurationEntity CreateConfiguration(int categoryCount = 1, string rootDirectory = "/scrapes/root", string famousRootDirectory = "famous")
    {
        var scrapeConfigurationId = new ScrapeConfigurationId(Guid.CreateVersion7());
        var searchConfigurationId = new SearchConfigurationId(Guid.CreateVersion7());
        var categoryNames = new[] { "category one", "category two", "category three", "category four", "category five" };
        var categories = Enumerable.Range(1, categoryCount)
            .Select(i => new SearchCategoryEntity { SearchConfigurationId = searchConfigurationId, Id = $"cat{i}", Name = categoryNames[i - 1] })
            .ToList();

        var configuration = new ScrapeConfigurationEntity(scrapeConfigurationId)
        {
            BaseUrl = new Uri("https://example.test"),
            TopWallpapers = "top/",
            SearchStringPrefix = "search/%7Bid%7D/",
            UserConfiguration = new UserConfigurationEntity(new UserConfigurationId(Guid.CreateVersion7()), scrapeConfigurationId, "user@example.test", "user", "api-key"),
            SearchConfiguration = new SearchConfigurationEntity(searchConfigurationId, scrapeConfigurationId, "cats", 10, categories),
            ScrapeDirectories = new ScrapeDirectoriesEntity(new ScrapeDirectoriesId(Guid.CreateVersion7()), scrapeConfigurationId, rootDirectory, famousRootDirectory, "sub")
        };
        configuration.SearchConfiguration.PersonCategories.Add(new PersonCategoryEntity { SearchConfigurationId = searchConfigurationId, Name = "Celebrities" });
        configuration.SearchConfiguration.PersonCategories.Add(new PersonCategoryEntity { SearchConfigurationId = searchConfigurationId, Name = "Models" });

        return configuration;
    }
}
