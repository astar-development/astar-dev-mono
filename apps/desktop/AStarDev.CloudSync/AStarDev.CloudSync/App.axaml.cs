using System.Diagnostics.CodeAnalysis;
using AStar.Dev.Velopack.Publishing.Avalonia.Updates;
using AStarDev.CloudSync.Accounts;
using AStarDev.CloudSync.Classification;
using AStarDev.CloudSync.Logging;
using AStarDev.CloudSync.Sync;
using AStarDev.CloudSync.ViewModels.Notifications;
using AStarDev.CloudSync.Tray;
using AStarDev.CloudSync.ViewModels;
using AStarDev.CloudSync.ViewModels.DisplaySettings;
using AStarDev.CloudSync.ViewModels.Overview;
using AStarDev.FunctionalParadigm;
using AStarDev.LoggingExtensions;
using AStarDev.CloudSync.ViewModels.Tray;
using AStarDev.LoggingOTel;
using AStarDev.SingleInstance;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using Avalonia.Threading;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace AStarDev.CloudSync;

[ExcludeFromCodeCoverage]
public partial class App : Application, IDisposable
{
    private ServiceProvider? services;
    private readonly CancellationTokenSource shutdown = new();
    private Task recovery = Task.CompletedTask;
    private Task startup = Task.CompletedTask;
    private bool disposed;

    public override void Initialize() => AvaloniaXamlLoader.Load(this);

    public override void OnFrameworkInitializationCompleted()
    {
        services = BuildServices();
        _ = services.GetRequiredService<TelemetryProviders>();
        LogUnhandledExceptions(services.GetRequiredService<UnhandledExceptionLogger>());

        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            var guard = services.GetRequiredService<ISingleInstanceGuard>();
            var options = services.GetRequiredService<SingleInstanceOptions>();
            var claim = Task.Run(() => guard.ClaimAsync(options, CancellationToken.None), CancellationToken.None).GetAwaiter().GetResult();
            if (claim is InstanceClaim.HandedOff or InstanceClaim.NotAnswered)
                ExitAsSecondInstance();

            _ = Task.Run(() => services.GetRequiredService<IMigrationLockClearer>().ClearAsync(CancellationToken.None), CancellationToken.None).GetAwaiter().GetResult();

            var window = new MainWindow { DataContext = services.GetRequiredService<MainWindowViewModel>() };
            desktop.MainWindow = window;
            LogViewerHotkey.Attach(window, () => services.GetRequiredService<LogViewerLauncher>().Open());
            KeepRunningInTray(window);
            StartHiddenWhenAutostarted(window, desktop.Args);
            _ = services.GetRequiredService<SyncNotifier>();
            KeepAsRunningInstance(claim, window, desktop);
            _ = services.GetRequiredService<SyncOverviewViewModel>().LoadAsync(CancellationToken.None);
            _ = services.GetRequiredService<SignInStateOverviewReloader>();
            _ = services.GetRequiredService<SyncScheduleSettingsViewModel>().LoadAsync(CancellationToken.None);
            _ = services.GetRequiredService<AutostartSettingsViewModel>().LoadAsync(CancellationToken.None);
            _ = services.GetRequiredService<TrashRetentionSettingsViewModel>().LoadAsync(CancellationToken.None);
            _ = Task.Run(() => services.GetRequiredService<AccountLogLevels>().LoadAsync(services.GetRequiredService<IAccountLogLevelSettings>(), CancellationToken.None), CancellationToken.None);
            _ = services.GetRequiredService<AccountLogLevelsViewModel>().LoadAsync(CancellationToken.None);
            recovery = Task.Run(() => services.GetRequiredService<IInterruptedSyncRecovery>().RecoverAsync(shutdown.Token), shutdown.Token);
            startup = Task.Run(() => services.GetRequiredService<SyncStartup>().RunAsync(shutdown.Token), CancellationToken.None);
            _ = services.GetRequiredService<SyncRunOverviewReloader>();
            _ = Task.Run(() => services.GetRequiredService<ISignInStateRefresher>().RefreshAllAsync(CancellationToken.None), CancellationToken.None);

            services.GetRequiredService<IWorkflowTelemetry>().Record(TelemetryWorkflowFactory.CreateStartup(), TelemetryOutcomeFactory.CreateSucceeded());
            _ = services.GetRequiredService<IUpdateScheduler>().RunAsync(shutdown.Token);
            _ = Task.Run(SweepLogFiles, CancellationToken.None);
            _ = Task.Run(() => services.GetRequiredService<IUploadSessionStore>().RemoveExpiredAsync(services.GetRequiredService<TimeProvider>().GetUtcNow(), shutdown.Token), shutdown.Token);
        }

        base.OnFrameworkInitializationCompleted();
    }

    private void LogUnhandledExceptions(UnhandledExceptionLogger unhandled)
    {
        AppDomain.CurrentDomain.UnhandledException += (_, e) =>
        {
            if (e.ExceptionObject is Exception exception)
                unhandled.Log(nameof(AppDomain), exception);

            if (e.IsTerminating)
                services?.Dispose();
        };
        TaskScheduler.UnobservedTaskException += (_, e) => unhandled.Log(nameof(TaskScheduler), e.Exception.GetBaseException());
    }

    private static void StartHiddenWhenAutostarted(Window window, string[]? arguments)
    {
        if (arguments?.Contains("--minimized", StringComparer.Ordinal) == true)
            window.Opened += (_, _) => window.Hide();
    }

    private void SweepLogFiles()
    {
        var sweeper = services?.GetRequiredService<LogFileSweeper>();
        var folder = services?.GetRequiredService<ILogFileLocator>().GetLogFolder();
        if (sweeper is not null && folder is not null)
        {
            _ = sweeper.DeleteOlderThan(folder, CloudSyncLogging.FileNamePrefix, CloudSyncLogging.Retention);
            _ = LogFileSweeper.DeleteBeyondTotalSize(folder, CloudSyncLogging.FileNamePrefix, CloudSyncLogging.MaxTotalBytes);
        }
    }

    private void KeepRunningInTray(Window window)
    {
        if (services is null)
            return;

        services.GetRequiredService<MainWindowVisibility>().Attach(window);
        var shutdownRequests = services.GetRequiredService<IAppShutdown>();
        var closePolicy = services.GetRequiredService<WindowClosePolicy>();
        window.Closing += (_, closing) => HideInsteadOfClosing(shutdownRequests, closePolicy, closing);
        services.GetRequiredService<TrayIconService>().Start(this);
    }

    private static void HideInsteadOfClosing(IAppShutdown shutdownRequests, WindowClosePolicy closePolicy, WindowClosingEventArgs closing)
    {
        if (shutdownRequests.State is not ShutdownState.Running || closing.CloseReason != WindowCloseReason.WindowClosing)
            return;

        closing.Cancel = true;
        _ = closePolicy.CloseAsync(CancellationToken.None);
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
        shutdown.Cancel();
        StopScheduler();
        services?.Dispose();
        Task.Run(() => instance.DisposeAsync().AsTask(), CancellationToken.None).GetAwaiter().GetResult();
    }

    private void StopScheduler()
    {
        if (services is null)
            return;

        using var patience = new CancellationTokenSource(TimeSpan.FromSeconds(10));
        Task.Run(() => services.GetRequiredService<ISyncScheduler>().StopAsync(patience.Token), CancellationToken.None).GetAwaiter().GetResult();
        Task.Run(async () => await recovery.WaitAsync(patience.Token).ConfigureAwait(ConfigureAwaitOptions.SuppressThrowing), CancellationToken.None).GetAwaiter().GetResult();
        Task.Run(async () => await startup.WaitAsync(patience.Token).ConfigureAwait(ConfigureAwaitOptions.SuppressThrowing), CancellationToken.None).GetAwaiter().GetResult();
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

        var logLevels = new AccountLogLevels();
        var services = new ServiceCollection();
        _ = services.AddSingleton(logLevels);
        _ = services.AddLogging(logging => CloudSyncLogging.Configure(logging.ConfigureOTelLogging(configuration, serviceName: "AStarDev.CloudSync"), logLevels, CloudSyncLogging.FolderPath()));
        _ = services.AddSingleton(_ => OTelTelemetryConfigurator.ConfigureOTelTelemetry(configuration, "AStarDev.CloudSync", WorkflowTelemetry.SourceName, WorkflowTelemetry.SourceName));
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

        if (!disposing)
            return;

        shutdown.Dispose();
        services?.Dispose();
    }
}
