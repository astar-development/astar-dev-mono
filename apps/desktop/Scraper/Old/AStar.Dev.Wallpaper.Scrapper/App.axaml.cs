using System.Globalization;
using System.IO.Abstractions;
using AStar.Dev.FunctionalParadigm;
using AStar.Dev.Infrastructure.AppDb;
using AStar.Dev.Infrastructure.AppDb.Entities;
using AStar.Dev.Wallpaper.Scrapper.Classifications;
using AStar.Dev.Wallpaper.Scrapper.Models;
using AStar.Dev.Wallpaper.Scrapper.Pages;
using AStar.Dev.Wallpaper.Scrapper.Repositories;
using AStar.Dev.Wallpaper.Scrapper.ScrapeConfigurationEditor;
using AStar.Dev.Wallpaper.Scrapper.Services;
using AStar.Dev.Wallpaper.Scrapper.Support;
using AStar.Dev.Wallpaper.Scrapper.Tags;
using AStar.Dev.Wallpaper.Scrapper.Workflows;
using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Serilog;
using Serilog.Exceptions;
using Testably.Abstractions;

namespace AStar.Dev.Wallpaper.Scrapper;

public partial class App : Application
{
    private IHost _host = null!;

    public static new App Current => (App)Application.Current!;
    public IServiceProvider Services => _host.Services;

    public override void Initialize() => AvaloniaXamlLoader.Load(this);

    public override async void OnFrameworkInitializationCompleted()
    {
        var builder = Host.CreateApplicationBuilder(new HostApplicationBuilderSettings
        {
            ContentRootPath = ApplicationMetadata.ApplicationFolder
        });

        builder.Configuration.AddUserSecrets<App>(optional: true, reloadOnChange: true);

        builder.Services
            .AddSingleton(sp =>
            {
                using var ctx = sp.GetRequiredService<IDbContextFactory<AppDbContext>>().CreateDbContext();

                return ctx.ScrapeConfiguration.GetScrapeConfigurations().ToAppModel();
            })
            .AddSingleton(sp => sp.GetRequiredService<ScrapeConfiguration>().SearchConfiguration)
            .AddSingleton<LogBroadcaster>()
            .AddSingleton<ImageBroadcaster>()
            .AddSingleton(sp =>
            {
                var broadcaster = sp.GetRequiredService<LogBroadcaster>();
                return new LoggerConfiguration()
                    .MinimumLevel.Debug()
                    .WriteTo.Console(formatProvider: CultureInfo.InvariantCulture)
                    .WriteTo.Seq("http://localhost:5341", formatProvider: CultureInfo.InvariantCulture)
                    .WriteTo.Sink(new StatusLogSink(broadcaster.Broadcast), Serilog.Events.LogEventLevel.Information)
                    .Enrich.WithExceptionDetails()
                    .Enrich.FromLogContext()
                    .ReadFrom.Configuration(sp.GetRequiredService<IConfiguration>())
                    .CreateLogger();
            })
            .AddSingleton<ILogger>(sp => sp.GetRequiredService<Serilog.Core.Logger>())
            .AddDbContextFactory<AppDbContext>(options => options.UseSqlite(SqliteConnectionStringProvider.Get(builder.Configuration)))
            .AddSingleton(sp =>
            {
                using var ctx = sp.GetRequiredService<IDbContextFactory<AppDbContext>>().CreateDbContext();
                return TagsFactory.LoadTagsToIgnoreCompletely(ctx);
            })
            .AddSingleton(sp =>
            {
                using var ctx = sp.GetRequiredService<IDbContextFactory<AppDbContext>>().CreateDbContext();
                return TagsFactory.LoadTagsTextToIgnore(ctx);
            })
            .AddTransient<IScrapedTagRepository, ScrapedTagRepository>()
            .AddTransient<IFileDetailRepository, FileDetailRepository>()
            .AddTransient<IDatabaseResetRepository, DatabaseResetRepository>()
            .AddTransient<FileClassificationService>()
            .AddTransient<IScrapedTagService, ScrapedTagService>()
            .AddTransient<IDatabaseResetService, DatabaseResetService>()
            .AddTransient<ConfigurationSaver>()
            .AddTransient<DatabaseInitializationService>()
            .AddTransient<ScrapeConfigurationViewModel>()
            .AddViewFactory<ScrapeConfigurationView>()
            .AddViewFactory<ClassificationsView>()
            .AddViewFactory<TagsView>()
            .AddSingleton<IPlaywrightService, PlaywrightService>()
            .AddTransient<SearchWorkflow>()
            .AddTransient<SearchResultsPage>()
            .AddTransient<PagedScrapeRunner>()
            .AddTransient<SubscriptionsImagesListPage>()
            .AddTransient<SubscriptionsWorkflow>()
            .AddTransient<ITopWallpapersPage, TopWallpapersPage>()
            .AddTransient<TopWallpapersWorkflow>()
            .AddTransient<IImportExportService, ImportExportService>()
            .AddTransient<IFileSystem, RealFileSystem>()
            .AddTransient<ScrapeConfigurationService>()
            .AddTransient<ImagePageService>()
            .AddTransient<ImagePage>()
            .AddSingleton<IDirectoryHelper, DirectoryHelper>()
            .AddSingleton<IDelayStrategy, RandomDelayStrategy>()
            .AddTransient<MainWindow>()
            .AddTransient(_ => TimeProvider.System)
            .AddTransient<IImageSaver, ImageSaver>()
            .AddTransient<IImageDimensionReader, ImageDimensionReader>()
            .AddHttpClient<IImageRetriever, ImageRetriever>(client => client.Timeout = TimeSpan.FromMinutes(2));

        _host = builder.Build();

        await _host.Services.GetRequiredService<DatabaseInitializationService>().InitialiseAsync();

        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            desktop.MainWindow = _host.Services.GetRequiredService<MainWindow>();
            desktop.Exit += OnExit;
            desktop.MainWindow.Show();
        }

        _host.Start();
        SurfaceConfigurationErrors();
        base.OnFrameworkInitializationCompleted();
    }

    private void SurfaceConfigurationErrors()
        => ScrapeConfigurationValidator.Validate(_host.Services.GetRequiredService<ScrapeConfiguration>())
            .Match(_ => Unit.Value, errors =>
            {
                var broadcaster = _host.Services.GetRequiredService<LogBroadcaster>();

                foreach (var error in errors)
                    broadcaster.Broadcast($"Configuration error - {error.Property}: {error.Message}");

                return Unit.Value;
            });

    private void OnExit(object? sender, ControlledApplicationLifetimeExitEventArgs e)
        => _host.StopAsync().GetAwaiter().GetResult();
}
