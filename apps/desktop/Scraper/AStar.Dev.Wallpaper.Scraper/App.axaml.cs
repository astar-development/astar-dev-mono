using AStar.Dev.Infrastructure.AppDb;
using AStar.Dev.Wallpaper.Scraper.Services;
using AStar.Dev.Wallpaper.Scraper.Startup;
using AStar.Dev.Wallpaper.Scraper.Theming;
using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Serilog;
using System.IO.Abstractions;
using Testably.Abstractions;
using System.Diagnostics.CodeAnalysis;

namespace AStar.Dev.Wallpaper.Scraper;

/// <summary>The Avalonia application entry point: bootstraps configuration, logging, dependency injection, and the main window.</summary>
[ExcludeFromCodeCoverage]
public partial class App : Application, IDisposable
{
    private bool disposedValue;

    private ServiceProvider? services;

    /// <summary>Loads the application's XAML resources.</summary>
    public override void Initialize() => AvaloniaXamlLoader.Load(this);

    /// <summary>Builds configuration, logging, and the dependency injection container, migrates the database, and shows the main window.</summary>
    public override void OnFrameworkInitializationCompleted()
    {
        services = BuildServices();
        MigrateDatabase(services);
        services.GetRequiredService<IThemeService>().Initialize();
        ShowMainWindow(services);

        base.OnFrameworkInitializationCompleted();
    }

    private static ServiceProvider BuildServices()
    {
        IFileSystem fileSystem = new RealFileSystem();
        var configuration = ApplicationConfigurationFactory.Build(AppContext.BaseDirectory);

        var collection = new ServiceCollection()
            .AddApplicationServices(configuration);

        ApplicationOptionsRegistrar.Register(collection, configuration);
        Log.Logger = SerilogConfigurator.CreateLogger(fileSystem, configuration);

        return collection
            .AddLogging(logging => logging.AddSerilog(dispose: true))
            .BuildServiceProvider();
    }

    private static void MigrateDatabase(ServiceProvider serviceProvider) =>
        DatabaseMigrator.MigrateAsync(
            serviceProvider.GetRequiredService<IDbContextFactory<AppDbContext>>(),
            serviceProvider.GetRequiredService<ILogger<App>>()).GetAwaiter().GetResult();

    private void ShowMainWindow(ServiceProvider serviceProvider)
    {
        if (ApplicationLifetime is not IClassicDesktopStyleApplicationLifetime desktop)
        {
            return;
        }

        desktop.MainWindow = serviceProvider.GetRequiredService<Home.MainWindow>();

        _ = serviceProvider.GetRequiredService<UpdateService>().CheckForUpdatesAsync(desktop.MainWindow);
    }

    /// <summary>Releases the resources held by the application's dependency injection container.</summary>
    /// <param name="disposing">Whether managed resources should be released.</param>
    protected virtual void Dispose(bool disposing)
    {
        if (!disposedValue)
        {
            if (disposing)
            {
                services?.Dispose();
            }

            disposedValue = true;
        }
    }

    /// <summary>Releases the resources held by the application's dependency injection container.</summary>
    public void Dispose()
    {
        // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method - Do NOT remove this comment.
        Dispose(disposing: true);
        GC.SuppressFinalize(this);
    }
}
