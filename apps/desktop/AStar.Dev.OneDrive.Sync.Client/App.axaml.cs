using System.Globalization;
using AStar.Dev.OneDrive.Sync.Client.Home;
using AStar.Dev.OneDrive.Sync.Client.Splash;
using AStar.Dev.OneDrive.Sync.Client.Infrastructure.Shell;
using AStar.Dev.OneDrive.Sync.Client.Data;
using AStar.Dev.OneDrive.Sync.Client.LogViewer;
using AStar.Dev.OneDrive.Sync.Client.Startup;
using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Serilog;
using Serilog.Events;
using Testably.Abstractions;
using AStar.Dev.OneDrive.Sync.Client.Infrastructure.ApplicationConfiguration;
using AStar.Dev.OneDrive.Sync.Client.Infrastructure.Startup;

namespace AStar.Dev.OneDrive.Sync.Client;

public class App : Application, IDisposable
{
    private readonly ServiceProvider _services = BuildServiceProvider();
    private readonly CancellationTokenSource _appLifetimeCts = new();
    private bool _disposed;

    public override void Initialize() => AvaloniaXamlLoader.Load(this);

    public override void OnFrameworkInitializationCompleted()
    {
        base.OnFrameworkInitializationCompleted();

        if (ApplicationLifetime is not IClassicDesktopStyleApplicationLifetime desktop)
            return;

        var splashWindow = _services.GetRequiredService<SplashWindow>();
        desktop.MainWindow = splashWindow;

        splashWindow.Opened += async (_, _) =>
        {
            var progress = new Progress<string>(splashWindow.SetStatus);
            var bootstrapper = _services.GetRequiredService<IAppBootstrapper>();
            await bootstrapper.BootstrapAsync(progress);
            var mainWindow = _services.GetRequiredService<MainWindow>();
            desktop.MainWindow = mainWindow;
            mainWindow.Show();
            splashWindow.Close();
        };

        desktop.Exit += async (_, _) =>
        {
            Log.Information("[App] Application exiting");
            await Log.CloseAndFlushAsync();
        };
    }

    private static ServiceProvider BuildServiceProvider()
    {
        var inMemoryLogSink = new InMemoryLogSink();
        var fileSystem = new RealFileSystem();

        var services = new ServiceCollection();

        _ = services.AddLogging(logging => logging.AddSerilog(dispose: true));
        _ = services.AddPersistence();
        _ = services.AddLocalizationServices();
        _ = services.AddStartupTasks();
        _ = services.AddViews();
        _ = services.AddViewModels();
        var configuration = RegisterOptions(services);
        ConfigureSerilog(inMemoryLogSink, fileSystem, configuration);

        _ = services.AddShell(inMemoryLogSink);

        return services.BuildServiceProvider();
    }

    private static IConfigurationRoot RegisterOptions(ServiceCollection services)
    {
        var configuration = new ConfigurationBuilder()
            .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
            .Build();
        _ = services.AddOptions<EntraIdConfiguration>()
                .Bind(configuration.GetSection("EntraId"))
                .ValidateDataAnnotations()
                .ValidateOnStart();
        _ = services.AddOptions<SyncSettings>()
                .Bind(configuration.GetSection("Sync"))
                .ValidateDataAnnotations()
                .ValidateOnStart();

        return configuration;
    }

    private static void ConfigureSerilog(InMemoryLogSink inMemoryLogSink, RealFileSystem fileSystem, IConfigurationRoot configuration)
    {
        _ = fileSystem.Directory.CreateDirectory(ApplicationDirectories.LogsDirectory);

        Log.Logger = new LoggerConfiguration()
            .MinimumLevel.Debug()
            .MinimumLevel.Override("Microsoft", LogEventLevel.Warning)
            .ReadFrom.Configuration(configuration)
            .WriteTo.Console(formatProvider: CultureInfo.InvariantCulture)
            .WriteTo.File(
                formatter: new Serilog.Formatting.Json.JsonFormatter(),
                path: $"{ApplicationDirectories.LogsDirectory}/log.txt",
                rollingInterval: RollingInterval.Day,
                retainedFileCountLimit: 7,
                shared: true,
                flushToDiskInterval: TimeSpan.FromSeconds(1))
            .WriteTo.Sink(inMemoryLogSink)
            .CreateLogger();
    }

    public void Dispose()
    {
        if (_disposed)
            return;

        _disposed = true;
        _appLifetimeCts.Dispose();
        _services.Dispose();

        GC.SuppressFinalize(this);
    }
}
