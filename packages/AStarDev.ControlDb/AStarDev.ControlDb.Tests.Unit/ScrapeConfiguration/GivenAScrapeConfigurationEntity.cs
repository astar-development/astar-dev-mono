using AStarDev.ControlDb.ScrapeConfiguration;
using AStarDev.Utilities;

namespace AStarDev.ControlDb.Tests.Unit.ScrapeConfiguration;

public class GivenAScrapeConfigurationEntity
{
    [Fact]
    public void when_properties_are_set_correctly_the_properties_are_assigned_as_expected()
    {
        string sut = CreateSut().ToJson() + Environment.NewLine;
        sut.ShouldMatchApproved();
    }

    private static ScrapeConfigurationEntity CreateSut()
    {
        var scrapeConfigurationId = new ScrapeConfigurationId(Guid.Empty);
        var connectionStringId = new ConnectionStringId(Guid.Empty);
        var userConfigurationId = new UserConfigurationId(Guid.Empty);
        var searchConfigurationId = new SearchConfigurationId(Guid.Empty);
        var scrapeDirectoriesId = new ScrapeDirectoriesId(Guid.Empty);
        var scrapeConfiguration = new ScrapeConfigurationEntity(scrapeConfigurationId)
        {
            ConnectionStrings = new ConnectionStringsEntity(connectionStringId, scrapeConfigurationId, "connection-string"),
            UserConfiguration = new UserConfigurationEntity(userConfigurationId, scrapeConfigurationId, "user@example.com", "username", "password", "session-cookie"),
            SearchConfiguration = new SearchConfigurationEntity(searchConfigurationId, scrapeConfigurationId, "search-config", "mock-category", 10),
            ScrapeDirectories = new ScrapeDirectoriesEntity(scrapeDirectoriesId, scrapeConfigurationId, "scrape-directory", "base-save-directory", "base-directory", "base-directory-famous", "sub-directory-name")
        };

        return scrapeConfiguration;
    }
}
