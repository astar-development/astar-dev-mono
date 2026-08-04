using System.Diagnostics.CodeAnalysis;
using System.IO.Abstractions;
using AStarDev.LoggingSerilog;
using AStarDev.WallpaperScraper.Home;
using AStarDev.WallpaperScraper.Startup;
using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Microsoft.Extensions.DependencyInjection;
using Serilog;
using Testably.Abstractions;

namespace AStarDev.WallpaperScraper;

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
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            desktop.MainWindow = new MainWindow();
        }
        services = BuildServices();

        base.OnFrameworkInitializationCompleted();
    }

    private static ServiceProvider BuildServices()
    {
        IFileSystem fileSystem = new RealFileSystem();
        var configuration = ApplicationConfigurationFactory.Build(AppContext.BaseDirectory);
        var collection = new ServiceCollection().AddApplicationServices(configuration);

        ApplicationOptionsRegistrar.Register(collection, configuration);
        Log.Logger = SerilogConfigurator.CreateLogger(configuration, $"{ApplicationDirectories.LogsDirectory}/{ApplicationMetadata.ApplicationLogName}", RollingInterval.Hour, 7);

        var serviceProvider = collection
            .AddInfrastructureServices()
            .AddApplicationServices(configuration)
            .AddLogging(logging => logging.AddSerilog(dispose: true))
            .BuildServiceProvider();
        var applicationDirectories = serviceProvider.GetRequiredService<IApplicationDirectories>();
        applicationDirectories.CreateIfRequired();
        Log.Information("Application directories created if required...");

        return serviceProvider;
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
