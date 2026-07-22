using AStar.Dev.Infrastructure.AppDb;
using AStar.Dev.Wallpaper.Scraper.Configuration;
using AStar.Dev.Wallpaper.Scraper.Home;
using AStar.Dev.Wallpaper.Scraper.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace AStar.Dev.Wallpaper.Scraper.Tests.Unit;

public class GivenAddApplicationServices
{
    private readonly ServiceProvider serviceProvider;

    public GivenAddApplicationServices()
    {
        IConfiguration configuration = new ConfigurationBuilder()
            .SetBasePath(AppContext.BaseDirectory)
            .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
            .Build();

        var services = new ServiceCollection()
            .AddApplicationServices(configuration)
            .AddLogging();

        serviceProvider = services.BuildServiceProvider();
    }

    [Fact]
    public void when_services_are_built_then_scrape_configuration_binds_from_the_test_appsettings()
    {
        var scrapeConfiguration = serviceProvider.GetRequiredService<IOptions<ScrapeConfiguration>>().Value;

        scrapeConfiguration.ApplicationName.ShouldBe("AStar Dev Wallpaper Scraper");
        scrapeConfiguration.ConnectionStrings.Sqlite.ShouldBe("Data Source=./tests-database.db");
    }

    [Fact]
    public void when_services_are_built_then_update_configuration_binds_from_appsettings()
    {
        var updateConfiguration = serviceProvider.GetRequiredService<IOptions<UpdateConfiguration>>().Value;

        updateConfiguration.RepositoryUrl.ShouldBe("https://github.com/astar-development/astar-dev-wallpaper-scraper");
    }

    [Fact]
    public void when_services_are_built_then_update_service_resolves() => serviceProvider.GetRequiredService<UpdateService>().ShouldNotBeNull();

    [Fact]
    public void when_app_db_context_factory_is_resolved_then_it_uses_the_configured_sqlite_connection_string()
    {
        var factory = serviceProvider.GetRequiredService<IDbContextFactory<AppDbContext>>();
        using var context = factory.CreateDbContext();

        context.Database.GetConnectionString().ShouldBe("Data Source=./tests-database.db");
    }

    [Fact]
    public void when_main_window_view_model_is_resolved_then_title_combines_appsettings_name_and_assembly_version()
    {
        var viewModel = serviceProvider.GetRequiredService<MainWindowViewModel>();

        viewModel.Title.ShouldBe($"AStar Dev Wallpaper Scraper V{MainWindowViewModel.ApplicationVersion}");
        MainWindowViewModel.ApplicationVersion.ShouldNotBe("0.0.0");
    }
}
