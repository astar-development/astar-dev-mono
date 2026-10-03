using AStarDev.ControlDb.ScrapeConfiguration;
using AStarDev.WallpaperScraper.ScrapeConfiguration;

namespace AStarDev.WallpaperScraper.TestsUnit.ScrapeConfiguration;

public sealed class GivenAScrapeConfigurationSummary
{
    [Fact]
    public void when_a_configuration_has_a_search_term_then_the_label_is_host_and_term()
    {
        var entity = CreateEntity(new Uri("https://wallhaven.cc"), "cats");

        var summary = ScrapeConfigurationSummary.From(entity);

        summary.Id.ShouldBe(entity.Id);
        summary.Label.ShouldBe("wallhaven.cc - cats");
    }

    [Fact]
    public void when_a_header_is_summarised_then_the_label_is_host_and_term()
    {
        var header = new ScrapeConfigurationHeader(new ScrapeConfigurationId(Guid.CreateVersion7()), new Uri("https://wallhaven.cc"), "cats");

        var summary = ScrapeConfigurationSummary.From(header);

        (summary.Id, summary.Label).ShouldBe((header.Id, "wallhaven.cc - cats"));
    }

    [Fact]
    public void when_the_search_term_is_empty_then_the_label_is_the_host_only()
    {
        var summary = ScrapeConfigurationSummary.From(CreateEntity(new Uri("https://wallhaven.cc"), string.Empty));

        summary.Label.ShouldBe("wallhaven.cc");
    }

    [Fact]
    public void when_the_search_term_is_whitespace_then_the_label_is_the_host_only()
    {
        var summary = ScrapeConfigurationSummary.From(CreateEntity(new Uri("https://wallhaven.cc"), "   "));

        summary.Label.ShouldBe("wallhaven.cc");
    }

    [Fact]
    public void when_the_base_url_is_relative_then_the_label_uses_the_original_text()
    {
        var summary = ScrapeConfigurationSummary.From(CreateEntity(new Uri("relative/path", UriKind.Relative), "cats"));

        summary.Label.ShouldBe("relative/path - cats");
    }

    [Fact]
    public void when_converted_to_text_then_the_label_is_returned()
    {
        var summary = new ScrapeConfigurationSummary(new ScrapeConfigurationId(Guid.CreateVersion7()), "wallhaven.cc - cats");

        summary.ToString().ShouldBe("wallhaven.cc - cats");
    }

    private static ScrapeConfigurationEntity CreateEntity(Uri baseUrl, string searchTerm)
    {
        var scrapeConfigurationId = new ScrapeConfigurationId(Guid.CreateVersion7());

        return new ScrapeConfigurationEntity(scrapeConfigurationId)
        {
            BaseUrl = baseUrl,
            SearchConfiguration = new SearchConfigurationEntity(new SearchConfigurationId(Guid.CreateVersion7()), scrapeConfigurationId, searchTerm, 10, [])
        };
    }
}
