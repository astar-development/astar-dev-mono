using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using AStar.Dev.Infrastructure.FilesDb.Data;
using AStar.Dev.Wallpaper.Scrapper.Classifications;
using AStar.Dev.Wallpaper.Scrapper.Models;
using AStar.Dev.Wallpaper.Scrapper.Repositories;
using AStar.Dev.Wallpaper.Scrapper.ScrapeConfigurationEditor;
using AStar.Dev.Wallpaper.Scrapper.Services;
using AStar.Dev.Wallpaper.Scrapper.Support;
using AStar.Dev.Wallpaper.Scrapper.Tags;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Serilog;
using Serilog.Exceptions;
using AStar.Dev.Wallpaper.Scrapper.Pages;
using AStar.Dev.Wallpaper.Scrapper.Workflows;
using System.Globalization;
using Testably.Abstractions;
using System.IO.Abstractions;

namespace AStar.Dev.Wallpaper.Scrapper;

public partial class App : Application
{
    private IHost _host = null!;

    public static new App Current => (App)Application.Current!;
    public IServiceProvider Services => _host.Services;

    public override void Initialize() => AvaloniaXamlLoader.Load(this);

    public override void OnFrameworkInitializationCompleted()
    {
        var builder = Host.CreateApplicationBuilder(new HostApplicationBuilderSettings
        {
            ContentRootPath = ApplicationMetadata.ApplicationFolder
        });

        builder.Configuration.AddUserSecrets<App>(optional: true, reloadOnChange: true);

        builder.Services
            .AddSingleton(sp =>
            {
                using var ctx = sp.GetRequiredService<IDbContextFactory<FilesContext>>().CreateDbContext();

                return ctx.ScrapeConfiguration
                    .Include(e => e.ConnectionStrings)
                    .Include(e => e.UserConfiguration)
                    .Include(e => e.SearchConfiguration).ThenInclude(s => s.SearchCategories)
                    .Include(e => e.ScrapeDirectories)
                    .OrderByDescending(e => e.Id)
                    .First()
                    .ToAppModel();
            })
            .AddSingleton<LogBroadcaster>()
            .AddSingleton<ImageBroadcaster>()
            .AddSingleton(sp => {
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
            .AddSingleton<Serilog.ILogger>(sp => sp.GetRequiredService<Serilog.Core.Logger>())
            .AddDbContextFactory<FilesContext>(options =>
                options.UseSqlite(builder.Configuration["scrapeConfiguration:connectionStrings:sqlite"]))
            .AddSingleton(sp => {
                using var ctx = sp.GetRequiredService<IDbContextFactory<FilesContext>>().CreateDbContext();
                return TagsFactory.LoadTagsToIgnoreCompletely(ctx);
            })
            .AddSingleton(sp => {
                using var ctx = sp.GetRequiredService<IDbContextFactory<FilesContext>>().CreateDbContext();
                return TagsFactory.LoadTagsTextToIgnore(ctx);
            })
            .AddTransient<IScrapedTagRepository, ScrapedTagRepository>()
            .AddTransient<IFileDetailRepository, FileDetailRepository>()
            .AddTransient<IDatabaseResetRepository, DatabaseResetRepository>()
            .AddTransient<FileClassificationService>()
            .AddTransient<IScrapedTagService, ScrapedTagService>()
            .AddTransient<IDatabaseResetService, DatabaseResetService>()
            .AddTransient<IImagePageResultFunctional, ImagePageResultFunctional>()
            .AddTransient<ConfigurationSaverFunctional>()
            .AddTransient<ConfigurationSaver>()
            .AddTransient<DatabaseInitializationService>()
            .AddTransient<ScrapeConfigurationViewModel>()
            .AddTransient<TopWallpapersWorkflowFunctional>()
            .AddTransient<ScrapeConfigurationView>()
            .AddTransient<ClassificationsView>()
            .AddTransient<TagsView>()
            .AddSingleton<IPlaywrightService, PlaywrightService>()
            .AddTransient<IImagePageServiceFunctional, ImagePageServiceFunctional>()
            .AddTransient<SearchWorkflowFunctional>()
            .AddTransient<SearchResultsPageFunctional>()
            .AddTransient<IImportExportService, ImportExportService>()
            .AddTransient<IFileSystem, RealFileSystem>()
            .AddTransient<ScrapeConfigurationService>()
            .AddTransient<ImagePageService>()
            .AddTransient<ImagePage>()
            .AddTransient<TimeProvider>(_ => TimeProvider.System)
            .AddTransient<Func<ScrapeConfigurationView>>(sp => () => sp.GetRequiredService<ScrapeConfigurationView>())
            .AddTransient<Func<ClassificationsView>>(sp => () => sp.GetRequiredService<ClassificationsView>())
            .AddTransient<Func<TagsView>>(sp => () => sp.GetRequiredService<TagsView>())
            .AddTransient<MainWindow>();

        _host = builder.Build();

        Task.Run(async () =>
            await _host.Services.GetRequiredService<DatabaseInitializationService>().InitialiseAsync()
        ).GetAwaiter().GetResult();

        if(ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            desktop.MainWindow = _host.Services.GetRequiredService<MainWindow>();
            desktop.Exit += OnExit;
        }

        _host.Start();
        base.OnFrameworkInitializationCompleted();
    }

    private void OnExit(object? sender, ControlledApplicationLifetimeExitEventArgs e)
        => _host.StopAsync().GetAwaiter().GetResult();
}
