using AStarDev.ScraperPlaying.Scraping;
using Microsoft.Extensions.Configuration;

namespace AStarDev.ScraperPlaying.TestsUnit.Scraping;

public sealed class GivenAScrapeLimits
{
    [Fact]
    public void when_the_defaults_are_used_then_all_categories_and_all_pages_are_allowed()
        => (ScrapeLimits.Default.MaximumSearchCategories, ScrapeLimits.Default.MaximumPagesPerSearch).ShouldBe((int.MaxValue, int.MaxValue));

    [Fact]
    public void when_the_configuration_has_no_scrape_limits_section_then_the_defaults_are_used()
    {
        var configuration = new ConfigurationBuilder().Build();

        ScrapeLimits.From(configuration).ShouldBe(ScrapeLimits.Default);
    }

    [Fact]
    public void when_the_configuration_sets_both_limits_then_they_are_used()
    {
        var configuration = Configuration(("ScrapeLimits:MaximumSearchCategories", "7"), ("ScrapeLimits:MaximumPagesPerSearch", "9"));

        ScrapeLimits.From(configuration).ShouldBe(new ScrapeLimits(7, 9));
    }

    [Fact]
    public void when_the_configuration_sets_only_one_limit_then_the_other_is_unlimited()
    {
        var configuration = Configuration(("ScrapeLimits:MaximumPagesPerSearch", "2"));

        ScrapeLimits.From(configuration).ShouldBe(new ScrapeLimits(int.MaxValue, 2));
    }

    [Theory]
    [InlineData("0")]
    [InlineData("-1")]
    public void when_a_configured_limit_is_less_than_one_then_that_limit_is_unlimited(string value)
    {
        var configuration = Configuration(("ScrapeLimits:MaximumSearchCategories", value), ("ScrapeLimits:MaximumPagesPerSearch", value));

        ScrapeLimits.From(configuration).ShouldBe(ScrapeLimits.Default);
    }

    private static IConfigurationRoot Configuration(params (string Key, string Value)[] values)
        => new ConfigurationBuilder().AddInMemoryCollection(values.Select(pair => new KeyValuePair<string, string?>(pair.Key, pair.Value))).Build();
}
