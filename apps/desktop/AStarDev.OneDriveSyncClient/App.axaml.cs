using System.Diagnostics.CodeAnalysis;
using AStarDev.OneDriveSyncClient.Data;
using AStarDev.OneDriveSyncClient.Home;
using AStarDev.OneDriveSyncClient.Infrastructure;
using AStarDev.OneDriveSyncClient.Infrastructure.ApplicationConfiguration;
using AStarDev.OneDriveSyncClient.Infrastructure.Shell;
using AStarDev.OneDriveSyncClient.Splash;
using AStarDev.OneDriveSyncClient.Startup;
using AStar.Dev.Velopack.Publishing;
using AStar.Dev.Velopack.Publishing.Avalonia.Updates;
using AStarDev.LoggingOTel;
using AStarDev.LoggingOTel.LogViewer;
using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using ApplicationMessages = AStar.Dev.Logging.Extensions.ApplicationMessages;

namespace AStarDev.OneDriveSyncClient;

[ExcludeFromCodeCoverage]
public class App : Application, IDisposable
{
    private ServiceProvider? services;
    private bool disposed;

    public override void Initialize() => AvaloniaXamlLoader.Load(this);

    public override void OnFrameworkInitializationCompleted()
    {
        base.OnFrameworkInitializationCompleted();

        services = BuildServiceProvider();

        if (ApplicationLifetime is not IClassicDesktopStyleApplicationLifetime desktop)
            return;

        var splashWindow = services.GetRequiredService<SplashWindow>();
        desktop.MainWindow = splashWindow;

        splashWindow.Opened += async (_, _) =>
        {
            var progress = new Progress<string>(splashWindow.SetStatus);
            var bootstrapper = services.GetRequiredService<IAppBootstrapper>();
            await bootstrapper.BootstrapAsync(progress);
            var mainWindow = services.GetRequiredService<MainWindow>();
            desktop.MainWindow = mainWindow;
            mainWindow.Show();
            splashWindow.Close();

            _ = services.GetRequiredService<IUpdateNotificationService>().CheckAndNotifyAsync();
        };

        desktop.Exit += (_, _) =>
        {
            var logger = services.GetRequiredService<ILogger<App>>();
            ApplicationMessages.Stopping(logger, ApplicationMetadata.ApplicationName);
            services.Dispose();
        };
    }

    private static ServiceProvider BuildServiceProvider()
    {
        var inMemoryLogProcessor = new InMemoryLogProcessor();

        var services = new ServiceCollection();

        _ = services.AddPersistence();
        _ = services.AddLocalizationServices();
        _ = services.AddStartupTasks();
        _ = services.AddViews();
        _ = services.AddViewModels();
        var configuration = RegisterOptions(services);
        _ = services.AddLogging(logging => logging.ConfigureOTelLogging(configuration, inMemoryLogProcessor));

        _ = services.AddVelopackUpdates(configuration);
        _ = services.AddShell(inMemoryLogProcessor);

        return services.BuildServiceProvider();
    }

    public static IConfigurationRoot RegisterOptions(ServiceCollection services)
    {
        var configuration = new ConfigurationBuilder()
            .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
            .Build();
        _ = services.AddOptions<EntraIdConfiguration>()
                .Bind(configuration.GetSection(EntraIdConfiguration.SectionName))
                .ValidateDataAnnotations()
                .ValidateOnStart();
        _ = services.AddOptions<SyncSettings>()
                .Bind(configuration.GetSection(SyncSettings.SectionName))
                .ValidateDataAnnotations()
                .ValidateOnStart();
        _ = services.AddOptions<ClientConfiguration>()
                .Bind(configuration.GetSection(ClientConfiguration.SectionName))
                .ValidateDataAnnotations()
                .ValidateOnStart();

        return configuration;
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    protected virtual void Dispose(bool disposing)
    {
        if (disposed)
            return;

        disposed = true;

        if (disposing)
            services?.Dispose();
    }
}
