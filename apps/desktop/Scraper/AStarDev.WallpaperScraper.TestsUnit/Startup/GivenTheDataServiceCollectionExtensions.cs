using AStarDev.ControlDb;
using AStarDev.WallpaperScraper.Scrapers;
using AStarDev.WallpaperScraper.Startup;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace AStarDev.WallpaperScraper.TestsUnit.Startup;

public sealed class GivenTheDataServiceCollectionExtensions
{
    [Fact]
    public void when_data_services_are_registered_then_the_scrape_configuration_repository_can_be_resolved() => CreateSut().GetRequiredService<IScrapeConfigurationRepository>().ShouldNotBeNull();

    [Fact]
    public void when_data_services_are_registered_then_the_control_db_context_can_be_resolved() => CreateSut().GetRequiredService<ControlDbContext>().ShouldNotBeNull();
    
    [Fact]
    public void when_data_services_are_registered_then_the_db_context_factory_can_be_resolved() => CreateSut().GetRequiredService<IDbContextFactory<ControlDbContext>>().ShouldNotBeNull();

    private static ServiceProvider CreateSut() => new ServiceCollection().AddDataServices().BuildServiceProvider();
}
