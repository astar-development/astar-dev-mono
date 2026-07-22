using AStar.Dev.Infrastructure.AppDb;
using AStar.Dev.Wallpaper.Scraper.Configuration;
using AStar.Dev.Wallpaper.Scraper.Configuration.EntityEditor;
using AStar.Dev.Wallpaper.Scraper.Scraping;
using AStar.Dev.Wallpaper.Scraper.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace AStar.Dev.Wallpaper.Scraper.Tests.Unit;

public class GivenServiceCollectionExtensions
{
    [Fact]
    public void when_application_services_are_added_then_the_playwright_service_is_registered_as_a_singleton()
    {
        var services = CreateServices();

        var descriptor = services.Single(service => service.ServiceType == typeof(IPlaywrightService));
        descriptor.Lifetime.ShouldBe(ServiceLifetime.Singleton);
        descriptor.ImplementationType.ShouldBe(typeof(PlaywrightService));
    }

    [Fact]
    public void when_application_services_are_added_then_the_app_db_context_factory_is_registered()
    {
        var services = CreateServices();

        services.ShouldContain(service => service.ServiceType == typeof(IDbContextFactory<AppDbContext>));
    }

    [Fact]
    public void when_application_services_are_added_then_the_search_category_scrape_action_is_registered_as_the_scrape_action()
    {
        var services = CreateServices();

        var descriptor = services.Single(service => service.ServiceType == typeof(IScrapeAction));
        descriptor.Lifetime.ShouldBe(ServiceLifetime.Singleton);
        descriptor.ImplementationType.ShouldBe(typeof(SearchCategoryScrapeAction));
    }

    [Fact]
    public void when_application_services_are_added_then_the_wallpaper_image_downloader_is_registered_without_a_circular_dependency()
    {
        var services = CreateServices();
        using var serviceProvider = services.BuildServiceProvider();

        var rawDownloader = serviceProvider.GetRequiredService<IRawWallpaperImageDownloader>();
        var decoratedDownloader = serviceProvider.GetRequiredService<IWallpaperImageDownloader>();

        rawDownloader.ShouldBeOfType<WallpaperImageDownloader>();
        decoratedDownloader.ShouldBeOfType<ThumbnailPublishingWallpaperImageDownloader>();
    }

    [Fact]
    public void when_application_services_are_added_then_the_entity_editor_factory_is_registered_as_a_simple_singleton()
    {
        var services = CreateServices();

        var descriptor = services.Single(service => service.ServiceType == typeof(IEntityEditorFactory));
        descriptor.Lifetime.ShouldBe(ServiceLifetime.Singleton);
        descriptor.ImplementationType.ShouldBe(typeof(EntityEditorFactory));
    }

    [Fact]
    public void when_application_services_are_added_then_an_export_directory_is_registered_as_a_singleton()
    {
        var services = CreateServices();
        using var serviceProvider = services.BuildServiceProvider();

        var exportDirectory = serviceProvider.GetRequiredService<ExportDirectory>();

        exportDirectory.Value.ShouldNotBeNullOrWhiteSpace();
    }

    private static IServiceCollection CreateServices()
    {
        IConfiguration configuration = new ConfigurationBuilder().Build();

        return new ServiceCollection().AddApplicationServices(configuration);
    }
}
