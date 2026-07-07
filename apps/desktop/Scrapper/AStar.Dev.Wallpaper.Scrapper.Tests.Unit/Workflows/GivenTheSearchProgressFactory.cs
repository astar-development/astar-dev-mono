using AStar.Dev.Wallpaper.Scrapper.Tests.Unit.TestData;
using AStar.Dev.Wallpaper.Scrapper.Workflows;

namespace AStar.Dev.Wallpaper.Scrapper.Tests.Unit.Workflows;

public sealed class GivenTheSearchProgressFactory
{
    [Fact]
    public void when_creating_a_search_progress_then_the_search_configuration_is_preserved()
    {
        var searchConfiguration = new SearchConfigurationBuilder().Build();
        var scrapeConfiguration = new ScrapeConfigurationBuilder { SearchConfiguration = searchConfiguration, }.Build();

        SearchProgressFactory.Create(searchConfiguration, scrapeConfiguration.ScrapeDirectories).SearchConfiguration.ShouldBeSameAs(searchConfiguration);
    }

    [Fact]
    public void when_creating_a_search_progress_then_the_scrape_directories_is_preserved()
    {
        var searchConfiguration = new SearchConfigurationBuilder().Build();
        var scrapeConfiguration = new ScrapeConfigurationBuilder { SearchConfiguration = searchConfiguration, }.Build();

        SearchProgressFactory.Create(searchConfiguration, scrapeConfiguration.ScrapeDirectories).ScrapeDirectories.ShouldBeSameAs(scrapeConfiguration.ScrapeDirectories);
    }
}
