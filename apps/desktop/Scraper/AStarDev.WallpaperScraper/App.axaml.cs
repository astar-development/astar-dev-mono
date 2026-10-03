using System.Diagnostics.CodeAnalysis;
using AStarDev.LoggingExtensions;
using AStarDev.LoggingOTel;
using AStarDev.WallpaperScraper.Startup;
using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using ApplicationMessages = AStarDev.LoggingExtensions.ApplicationMessages;

namespace AStarDev.WallpaperScraper;

[ExcludeFromCodeCoverage]
public partial class App : Application, IDisposable
{
    private bool disposed;
    private ServiceProvider? serviceProvider;

    public override void Initialize() => AvaloniaXamlLoader.Load(this);

    [SuppressMessage("Design", "CA1031:Do not catch general exception types", Justification = "Last-resort startup handler: any failure while building services must show the startup-error window instead of crashing.")]
    public override void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            try
            {
                serviceProvider = BuildServices();
                desktop.MainWindow = serviceProvider.GetRequiredService<UI.MainWindow>();
            }
            catch (Exception exception)
            {
                desktop.MainWindow = new UI.StartupErrorWindow(exception);
            }
        }

        base.OnFrameworkInitializationCompleted();
    }

    private static ServiceProvider BuildServices()
    {
        var configuration = ApplicationConfigurationFactory.Build(AppContext.BaseDirectory);
        var collection = new ServiceCollection().AddConfigurationServices(configuration);

        var serviceProvider = collection
            .AddDataServices()
            .AddInfrastructureServices()
            .AddApplicationServices(configuration)
            .AddLogging(builder => builder.ConfigureOTelLogging(configuration, serviceName: ApplicationMetadata.ApplicationName).AddFileLogging(ApplicationDirectories.LogsDirectory, ApplicationMetadata.ApplicationNameHyphenated))
            .BuildServiceProvider(new ServiceProviderOptions { ValidateScopes = true, ValidateOnBuild = true });

        var applicationDirectories = serviceProvider.GetRequiredService<IApplicationDirectories>();
        applicationDirectories.CreateIfRequired();
        var logger = serviceProvider.GetRequiredService<ILogger<App>>();
        ApplicationMessages.StartupSuccessful(logger, ApplicationMetadata.ApplicationName);
        StartDatabaseInitialization(serviceProvider);

        return serviceProvider;
    }

    // The migration runs in the background so the window appears immediately; MainWindow awaits the same task before touching the database and reports any failure.
    private static void StartDatabaseInitialization(ServiceProvider serviceProvider) =>
        _ = serviceProvider.GetRequiredService<IDatabaseInitialization>().ReadyAsync();

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
