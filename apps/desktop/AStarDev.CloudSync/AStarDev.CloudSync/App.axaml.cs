using System.Diagnostics.CodeAnalysis;
using AStar.Dev.Velopack.Publishing.Avalonia.Updates;
using AStarDev.CloudSync.Sync;
using AStarDev.CloudSync.ViewModels;
using AStarDev.CloudSync.ViewModels.Overview;
using AStarDev.LoggingOTel;
using AStarDev.SingleInstance;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Avalonia.Threading;
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

        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            var guard = services.GetRequiredService<ISingleInstanceGuard>();
            var options = services.GetRequiredService<SingleInstanceOptions>();
            var claim = Task.Run(() => guard.ClaimAsync(options, CancellationToken.None)).GetAwaiter().GetResult();
            if (claim is InstanceClaim.HandedOff or InstanceClaim.NotAnswered)
                ExitAsSecondInstance();

            var window = new MainWindow { DataContext = services.GetRequiredService<MainWindowViewModel>() };
            desktop.MainWindow = window;
            KeepAsRunningInstance(claim, window, desktop);
            _ = services.GetRequiredService<SyncOverviewViewModel>().LoadAsync(CancellationToken.None);
            _ = services.GetRequiredService<SignInStateOverviewReloader>();
            _ = Task.Run(() => services.GetRequiredService<ISignInStateRefresher>().RefreshAllAsync(CancellationToken.None), CancellationToken.None);

            _ = services.GetRequiredService<IUpdateNotificationService>().CheckAndNotifyAsync();
        }

        base.OnFrameworkInitializationCompleted();
    }

    private void ExitAsSecondInstance()
    {
        services?.Dispose();
        Environment.Exit(0);
    }

    private void KeepAsRunningInstance(InstanceClaim claim, Window window, IClassicDesktopStyleApplicationLifetime desktop)
    {
        if (claim is not InstanceClaim.Primary primary)
            return;

        var instance = primary.Instance;
        instance.ActivationRequested += (_, _) => Dispatcher.UIThread.Post(() => BringForward(window));
        desktop.Exit += (_, _) => ReleaseAfterServices(instance);
    }

    private void ReleaseAfterServices(PrimaryInstance instance)
    {
        services?.Dispose();
        Task.Run(() => instance.DisposeAsync().AsTask()).GetAwaiter().GetResult();
    }

    private static void BringForward(Window window)
    {
        window.Show();
        if (window.WindowState == WindowState.Minimized)
            window.WindowState = WindowState.Normal;

        window.Activate();
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
