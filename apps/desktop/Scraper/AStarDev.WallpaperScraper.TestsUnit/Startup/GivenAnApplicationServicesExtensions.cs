using System.IO.Abstractions;
using AStar.Dev.Velopack.Publishing;
using AStar.Dev.Velopack.Publishing.Avalonia.Updates;
using AStarDev.WallpaperScraper.Configuration;
using AStarDev.WallpaperScraper.Home;
using AStarDev.WallpaperScraper.Scrapers;
using AStarDev.WallpaperScraper.Services;
using AStarDev.WallpaperScraper.Startup;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Testably.Abstractions.Testing;

namespace AStarDev.WallpaperScraper.TestsUnit.Startup;

public class GivenAnApplicationServicesExtensions
{
    [Fact]
    public void when_application_directories_are_registered_then_they_can_be_resolved()
    {
        var sut = CreateSut();

        sut.GetRequiredService<IApplicationDirectories>().ShouldNotBeNull();
    }

    [Fact]
    public void when_update_dialog_text_provider_is_registered_then_it_can_be_resolved()
    {
        var sut = CreateSut();

        sut.GetRequiredService<IUpdateDialogTextProvider>().ShouldNotBeNull();
    }

    [Fact]
    public void when_playwright_service_is_registered_then_it_can_be_resolved()
    {
        var sut = CreateSut();

        sut.GetRequiredService<IPlaywrightService>().ShouldNotBeNull();
    }

    [Fact]
    public void when_velopack_update_settings_are_registered_then_they_can_be_resolved()
    {
        var sut = CreateSut();

        sut.GetRequiredService<IOptions<VelopackUpdateSettings>>().Value.ChannelPrefix.ShouldBe("SomeValue");
    }

    [Fact]
    public void when_velopack_update_service_is_registered_then_it_can_be_resolved()
    {
        var sut = CreateSut();

        sut.GetRequiredService<IVelopackUpdateService>().ShouldNotBeNull();
    }

    [Fact]
    public void when_scrape_orchestrator_is_registered_then_it_can_be_resolved()
    {
        var sut = CreateSut();

        sut.GetRequiredService<IScrapeOrchestrator>().ShouldNotBeNull();
    }

    [Fact]
    public void when_the_file_system_is_registered_then_it_can_be_resolved()
    {
        var sut = CreateSut();

        sut.GetRequiredService<IFileSystem>().ShouldNotBeNull();
    }

    private static ServiceProvider CreateSut()
    {
        var configuration = new ConfigurationBuilder()
                    .AddInMemoryCollection([
                        new($"{VelopackUpdateSettings.SectionName}:ChannelPrefix", "SomeValue"),
                new($"{VelopackUpdateSettings.SectionName}:GithubRepositoryUrl", "https://github.com/astar-development/astar-dev-mono"),
                new($"{ScrapeConfiguration.SectionName}:ApplicationName", "SomeValue")
                    ])
                    .Build();
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddApplicationServices(configuration);

        return services.BuildServiceProvider();
    }
}
