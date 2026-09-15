using System.Diagnostics.CodeAnalysis;
using AStar.Dev.FunctionalParadigm;
using AStarDev.ControlDb;
using AStarDev.LoggingOTel;
using AStarDev.WallpaperScraper.Home;
using AStarDev.WallpaperScraper.Startup;
using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using ApplicationMessages = AStar.Dev.Logging.Extensions.ApplicationMessages;
using LogMessage = AStar.Dev.Logging.Extensions.LogMessage;

namespace AStarDev.WallpaperScraper;

/// <summary>The Avalonia application entry point: bootstraps configuration, logging, dependency injection, and the main window.</summary>
[ExcludeFromCodeCoverage]
public partial class App : Application, IDisposable
{
    private bool disposed;
    private ServiceProvider? serviceProvider;

    /// <summary>Loads the application's XAML resources.</summary>
    public override void Initialize() => AvaloniaXamlLoader.Load(this);

    /// <summary>Builds configuration, logging, and the dependency injection container, migrates the database, and shows the main window.</summary>
    public override void OnFrameworkInitializationCompleted()
    {
        serviceProvider = BuildServices();
        if (ApplicationLifetime is not IClassicDesktopStyleApplicationLifetime desktop) return;

        desktop.MainWindow = serviceProvider.GetRequiredService<MainWindow>();

        base.OnFrameworkInitializationCompleted();
    }

    private static ServiceProvider BuildServices()
    {
        var configuration = ApplicationConfigurationFactory.Build(AppContext.BaseDirectory);
        var collection = new ServiceCollection().AddConfigurationServices(configuration).AddApplicationServices(configuration);

        var serviceProvider = collection
            .AddInfrastructureServices()
            .AddDataServices()
            .AddApplicationServices(configuration)
            .AddLogging(logging => logging.ConfigureOTelLogging(configuration))
            .BuildServiceProvider();
        var applicationDirectories = serviceProvider.GetRequiredService<IApplicationDirectories>();
        var startupDiagnostics = serviceProvider.GetRequiredService<StartupDiagnostics>();
        var logger = serviceProvider.GetRequiredService<ILogger<App>>();
        CreateApplicationDirectories(applicationDirectories, startupDiagnostics, logger);
        ApplicationMessages.StartupSuccessful(logger, ApplicationMetadata.ApplicationName);
        MigrateDatabase(serviceProvider, startupDiagnostics);

        return serviceProvider;
    }

    private static void CreateApplicationDirectories(IApplicationDirectories applicationDirectories, StartupDiagnostics startupDiagnostics, ILogger logger)
    {
        try
        {
            applicationDirectories.CreateIfRequired();
        }
        catch (Exception exception)
        {
            LogMessage.Error(logger, "Failed to create application directories", exception);
            startupDiagnostics.RecordFailure(StartupFailureFactory.Create("Application directories", exception));
        }
    }

    private static void MigrateDatabase(ServiceProvider serviceProvider, StartupDiagnostics startupDiagnostics) =>
        DatabaseMigrator.MigrateAsync(
            serviceProvider.GetRequiredService<IDbContextFactory<ControlDbContext>>(),
            serviceProvider.GetRequiredService<ILogger<App>>())
            .GetAwaiter().GetResult()
            .Match(static _ => UnitFp.Instance, exception =>
            {
                startupDiagnostics.RecordFailure(StartupFailureFactory.Create("Database migration", exception));

                return UnitFp.Instance;
            });


    /// <summary>Releases the resources held by the application's dependency injection container.</summary>
    /// <param name="disposing">Whether managed resources should be released.</param>
    protected virtual void Dispose(bool disposing)
    {
        if (disposed) return;

        if (disposing)
        {
            serviceProvider?.Dispose();
        }

        disposed = true;
    }

    /// <summary>Releases the resources held by the application's dependency injection container.</summary>
    public void Dispose()
    {
        // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method - Do NOT remove this comment.
        Dispose(disposing: true);
        GC.SuppressFinalize(this);
    }
}
