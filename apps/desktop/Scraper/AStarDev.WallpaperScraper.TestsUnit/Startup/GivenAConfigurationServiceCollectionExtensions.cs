using AStarDev.SourceGenerators.OptionsBindingGeneration;
using AStarDev.WallpaperScraper.Configuration;
using AStarDev.WallpaperScraper.Startup;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace AStarDev.WallpaperScraper.TestsUnit.Startup;

public class GivenAConfigurationServiceCollectionExtensions
{
    [Fact]
    public void when_configuration_is_registered_then_it_can_be_resolved()
    {
        var configuration = new ConfigurationBuilder().Build();
        var services = new ServiceCollection();

        services.AddConfigurationServices(configuration);

        var provider = services.BuildServiceProvider();
        var resolvedConfiguration = provider.GetRequiredService<IConfiguration>();

        resolvedConfiguration.ShouldBeSameAs(configuration);
    }

    [Fact]
    public void when_scrape_configuration_is_registered_then_it_can_be_resolved()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection([new($"{ScraperAppConfiguration.SectionName}:ApplicationName", "SomeValue")])
            .Build();
        var services = new ServiceCollection();
        services.AddConfigurationServices(configuration).AddAutoRegisteredOptions(configuration);

        var provider = services.BuildServiceProvider();
        var scraperAppConfiguration = provider.GetRequiredService<IOptions<ScraperAppConfiguration>>().Value;

        scraperAppConfiguration.ApplicationName.ShouldBe("SomeValue");
    }
}
