using System.IO.Abstractions;
using AStar.Dev.Infrastructure.AppDb;
using AStar.Dev.Wallpaper.Scraper.DTOs;
using AStar.Dev.Wallpaper.Scraper.Models;
using AStar.Dev.Wallpaper.Scraper.Pages;
using AStar.Dev.Wallpaper.Scraper.Repositories;
using AStar.Dev.Wallpaper.Scraper.Services;
using AStar.Dev.Wallpaper.Scraper.Support;
using AStar.Dev.Wallpaper.Scraper.Tests.Unit.TestData;
using AStar.Dev.Wallpaper.Scraper.Workflows;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Serilog;

namespace AStar.Dev.Wallpaper.Scraper.Tests.Unit.Workflows;

public sealed class GivenTheWorkflowServiceRegistrations
{
    private static ServiceProvider BuildServiceProvider()
    {
        var logger = new LoggerConfiguration().CreateLogger();

        var services = new ServiceCollection()
            .AddSingleton(new ScrapeConfigurationBuilder().Build())
            .AddSingleton(sp => sp.GetRequiredService<ScrapeConfiguration>().SearchConfiguration)
            .AddSingleton(logger)
            .AddSingleton<ILogger>(logger)
            .AddSingleton(new TagsToIgnoreCompletely())
            .AddSingleton(new TagsTextToIgnore())
            .AddSingleton(_ => Substitute.For<IDbContextFactory<AppDbContext>>())
            .AddSingleton(_ => Substitute.For<IPlaywrightService>())
            .AddSingleton(_ => Substitute.For<IFileDetailRepository>())
            .AddSingleton(_ => Substitute.For<IScrapedTagRepository>())
            .AddSingleton(_ => Substitute.For<IDirectoryHelper>())
            .AddSingleton(_ => Substitute.For<IImageRetriever>())
            .AddSingleton(_ => Substitute.For<IImageSaver>())
            .AddSingleton(_ => Substitute.For<IImageDimensionReader>())
            .AddSingleton<IFileSystem>(new MockFileSystem())
            .AddSingleton<IDelayStrategy, NoOpDelayStrategy>()
            .AddSingleton<ImageBroadcaster>()
            .AddTransient(_ => System.TimeProvider.System)
            .AddTransient<FileClassificationService>()
            .AddTransient<ImagePage>()
            .AddTransient<ImagePageService>()
            .AddTransient<ConfigurationSaver>()
            .AddTransient<PagedScrapeRunner>()
            .AddTransient<SubscriptionsImagesListPage>()
            .AddTransient<SubscriptionsWorkflow>()
            .AddTransient<ITopWallpapersPage, TopWallpapersPage>()
            .AddTransient<TopWallpapersWorkflow>();

        return services.BuildServiceProvider();
    }

    [Fact]
    public void when_resolving_the_subscriptions_workflow_then_resolution_succeeds()
    {
        using var serviceProvider = BuildServiceProvider();

        var subscriptionsWorkflow = serviceProvider.GetRequiredService<SubscriptionsWorkflow>();

        subscriptionsWorkflow.ShouldNotBeNull();
    }

    [Fact]
    public void when_resolving_the_top_wallpapers_workflow_then_resolution_succeeds()
    {
        using var serviceProvider = BuildServiceProvider();

        var topWallpapersWorkflow = serviceProvider.GetRequiredService<TopWallpapersWorkflow>();

        topWallpapersWorkflow.ShouldNotBeNull();
    }
}
