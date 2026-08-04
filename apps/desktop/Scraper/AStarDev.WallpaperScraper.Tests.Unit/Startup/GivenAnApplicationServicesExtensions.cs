using AStar.Dev.Velopack.Publishing;
using AStar.Dev.Velopack.Publishing.Avalonia.Updates;
using AStarDev.WallpaperScraper.Configuration;
using AStarDev.WallpaperScraper.Home;
using AStarDev.WallpaperScraper.Services;
using AStarDev.WallpaperScraper.Startup;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace AStarDev.WallpaperScraper.Tests.Unit.Startup;

public class GivenAnApplicationServicesExtensions
{
    [Fact]
    public void when_application_directories_are_registered_then_they_can_be_resolved()
    {
        var configuration = new ConfigurationBuilder().Build();
        var services = new ServiceCollection();

        services.AddApplicationServices(configuration);

        var provider = services.BuildServiceProvider();
        var resolvedService = provider.GetRequiredService<IApplicationDirectories>();

        resolvedService.ShouldNotBeNull();
    }

    [Fact]
    public void when_update_dialog_text_provider_is_registered_then_it_can_be_resolved()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection([new($"{ScrapeConfiguration.SectionName}:ApplicationName", "SomeValue")])
            .Build();
        var services = new ServiceCollection();
        services.AddApplicationServices(configuration);

        var provider = services.BuildServiceProvider();
        var resolvedService = provider.GetRequiredService<IUpdateDialogTextProvider>();

        resolvedService.ShouldNotBeNull();
    }

    [Fact]
    public void when_playwright_service_is_registered_then_it_can_be_resolved()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection([new($"{ScrapeConfiguration.SectionName}:ApplicationName", "SomeValue")])
            .Build();
        var services = new ServiceCollection();
        services.AddApplicationServices(configuration);

        var provider = services.BuildServiceProvider();
        var resolvedService = provider.GetRequiredService<IPlaywrightService>();

        resolvedService.ShouldNotBeNull();
    }

    [Fact]
    public void when_main_window_view_model_is_registered_then_it_can_be_resolved()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection([new($"{ScrapeConfiguration.SectionName}:ApplicationName", "SomeValue")])
            .Build();
        var services = new ServiceCollection();
        services.AddApplicationServices(configuration);

        var provider = services.BuildServiceProvider();
        var resolvedService = provider.GetRequiredService<MainWindowViewModel>();

        resolvedService.ShouldNotBeNull();
    }

    [Fact]
    public void when_main_window_is_registered_then_it_can_be_resolved()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection([new($"{ScrapeConfiguration.SectionName}:ApplicationName", "SomeValue")])
            .Build();
        var services = new ServiceCollection();
        services.AddApplicationServices(configuration);

        var provider = services.BuildServiceProvider();
        var resolvedService = provider.GetRequiredService<MainWindow>();

        resolvedService.ShouldNotBeNull();
    }

    [Fact]
    public void when_velopack_update_settings_are_registered_then_they_can_be_resolved()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection([new($"{VelopackUpdateSettings.SectionName}:ChannelPrefix", "SomeValue")])
            .Build();
        var services = new ServiceCollection();
        services.AddConfigurationServices(configuration);

        var provider = services.BuildServiceProvider();
        var velopackUpdateSettings = provider.GetRequiredService<IOptions<VelopackUpdateSettings>>().Value;

        velopackUpdateSettings.ChannelPrefix.ShouldBe("SomeValue");
    }

    [Fact]
    public void when_velopack_update_service_is_registered_then_it_can_be_resolved()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection([new($"{VelopackUpdateSettings.SectionName}:ChannelPrefix", "SomeValue")])
            .Build();
        var services = new ServiceCollection();
        services.AddApplicationServices(configuration);

        var provider = services.BuildServiceProvider();
        var resolvedService = provider.GetRequiredService<IVelopackUpdateService>();

        resolvedService.ShouldNotBeNull();
    }
}
