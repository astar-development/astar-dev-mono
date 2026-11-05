using System.Diagnostics.CodeAnalysis;
using AStar.Dev.Velopack.Publishing.Avalonia.Updates;
using AStarDev.CloudSync.Core.Persistence;
using AStarDev.CloudSync.ViewModels;
using AStarDev.CloudSync.ViewModels.Overview;
using AStarDev.LoggingOTel;
using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace AStarDev.CloudSync;

[ExcludeFromCodeCoverage]
public partial class App : Application, IDisposable
{
    private ServiceProvider? services;
    private bool disposed;

    public override void Initialize() => AvaloniaXamlLoader.Load(this);

    public override void OnFrameworkInitializationCompleted()
    {
        services = BuildServices();
        _ = services.GetRequiredService<ICloudSyncDatabase>().MigrateAsync(CancellationToken.None).GetAwaiter().GetResult();

        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            desktop.MainWindow = new MainWindow { DataContext = services.GetRequiredService<MainWindowViewModel>() };
            _ = services.GetRequiredService<SyncOverviewViewModel>().LoadAsync(CancellationToken.None);

            _ = services.GetRequiredService<IUpdateNotificationService>().CheckAndNotifyAsync();
        }

        base.OnFrameworkInitializationCompleted();
    }

    private static ServiceProvider BuildServices()
    {
        var configuration = new ConfigurationBuilder()
            .AddJsonFile("appsettings.json", optional: false, reloadOnChange: false)
            .AddJsonFile("appsettings.Debug.json", optional: true, reloadOnChange: false)
            .Build();

        var services = new ServiceCollection();
        _ = services.AddLogging(logging => logging.ConfigureOTelLogging(configuration, serviceName: "AStarDev.CloudSync"));
        _ = services.AddApplicationServices(configuration);

        return services.BuildServiceProvider();
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
