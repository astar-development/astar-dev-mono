using AStar.Dev.Wallpaper.Scraper.Configuration;
using AStar.Dev.Wallpaper.Scraper.Startup;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace AStar.Dev.Wallpaper.Scraper.Tests.Unit.Startup;

public class GivenApplicationOptionsRegistrar
{
    private static IConfiguration BuildConfiguration(string? progressReportInterval) =>
        new ConfigurationBuilder()
            .AddInMemoryCollection([new("Sync:ProgressReportInterval", progressReportInterval)])
            .Build();

    [Fact]
    public void when_configuration_has_a_valid_interval_then_sync_settings_bind_from_it()
    {
        var services = new ServiceCollection();
        ApplicationOptionsRegistrar.Register(services, BuildConfiguration("25"));

        var syncSettings = services.BuildServiceProvider().GetRequiredService<IOptions<SyncSettings>>().Value;

        syncSettings.ProgressReportInterval.ShouldBe(25);
    }

    [Fact]
    public void when_configured_interval_is_below_the_minimum_then_resolving_options_throws()
    {
        var services = new ServiceCollection();
        ApplicationOptionsRegistrar.Register(services, BuildConfiguration("0"));

        var provider = services.BuildServiceProvider();

        Should.Throw<OptionsValidationException>(() => provider.GetRequiredService<IOptions<SyncSettings>>().Value);
    }

    [Fact]
    public void when_configured_interval_is_above_the_maximum_then_resolving_options_throws()
    {
        var services = new ServiceCollection();
        ApplicationOptionsRegistrar.Register(services, BuildConfiguration("501"));

        var provider = services.BuildServiceProvider();

        Should.Throw<OptionsValidationException>(() => provider.GetRequiredService<IOptions<SyncSettings>>().Value);
    }
}
