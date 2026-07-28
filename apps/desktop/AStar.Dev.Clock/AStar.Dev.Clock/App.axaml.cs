using System.IO.Abstractions;
using AStar.Dev.Clock.Theming;
using AStar.Dev.Clock.Updates;
using AStar.Dev.Velopack.Publishing.Avalonia.Updates;
using AStarDev.LoggingSerilog;
using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Serilog;
using Testably.Abstractions;

namespace AStar.Dev.Clock;

public partial class App : Application, IDisposable
{
    private ServiceProvider? services;
    private bool disposed;

    public override void Initialize() => AvaloniaXamlLoader.Load(this);

    public override void OnFrameworkInitializationCompleted()
    {
        services = BuildServices();

        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            services.GetRequiredService<IThemeService>().Initialize();
            desktop.MainWindow = new MainWindow { DataContext = services.GetRequiredService<MainWindowViewModel>() };

            _ = services.GetRequiredService<IUpdateNotificationService>().CheckAndNotifyAsync();
        }

        base.OnFrameworkInitializationCompleted();
    }

    private static ServiceProvider BuildServices()
    {
        var configuration = new ConfigurationBuilder()
            .AddJsonFile("appsettings.json", optional: false, reloadOnChange: false)
            .Build();

        Log.Logger = SerilogConfigurator.CreateLogger(configuration, $"{ApplicationDirectories.LogsDirectory}/{ApplicationMetadata.ApplicationLogName}", RollingInterval.Hour, 7);

        var services = new ServiceCollection();
        _ = services.AddSingleton<IFileSystem, RealFileSystem>();
        _ = services.AddSingleton<IThemeService, ThemeService>();
        _ = services.AddScoped<MainWindowViewModel>();
        _ = services.AddLogging(logging => logging.AddSerilog(dispose: true));
        _ = services.AddVelopackUpdateServices(configuration);

        return services.BuildServiceProvider();
    }

    public void Dispose()
    {
        if (disposed)
            return;

        disposed = true;
        services?.Dispose();

        GC.SuppressFinalize(this);
    }
}
