using System;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Threading;
using System.Threading.Tasks;
using AStar.Dev.Functional.Extensions;
using AStar.Dev.OneDriveSync.Infrastructure.Persistence;
using Avalonia.Styling;
using Microsoft.Extensions.Logging;
using ReactiveUI;

using MelILogger = Microsoft.Extensions.Logging.ILogger;

namespace AStar.Dev.OneDriveSync.Infrastructure.Theming;

/// <summary>Manages app-wide theming: DB persistence, OS observation, and runtime switching (AC TH-01 to TH-06).</summary>
public sealed partial class ThemeService(IAppSettingsRepository settingsRepository, IApplicationThemeAdapter themeAdapter, IPlatformThemeProvider platformThemeProvider, ILogger<ThemeService> logger) : IThemeService, IDisposable
{
    private readonly Subject<ThemeMode> _themeChanged = new();
    private IDisposable? _platformSubscription;

    /// <inheritdoc />
    public ThemeMode CurrentMode { get; private set; } = ThemeMode.Auto;

    /// <inheritdoc />
    public IObservable<ThemeMode> ThemeChanged => _themeChanged.AsObservable();

    /// <inheritdoc />
    public async Task<Result<ThemeMode, ErrorResponse>> InitialiseAsync(CancellationToken ct = default)
    {
        var result = await settingsRepository.GetAsync(ct).ConfigureAwait(false);

        var mode = result is Result<AppSettings?, ErrorResponse>.Ok { Value: { } s }
            ? ParseMode(s.ThemeMode)
            : ThemeMode.Auto;

        ApplyMode(mode);
        LogThemeInitialised(logger, mode);

        return new Result<ThemeMode, ErrorResponse>.Ok(mode);
    }

    /// <inheritdoc />
    public async Task<Result<ThemeMode, ErrorResponse>> SetThemeAsync(ThemeMode mode, CancellationToken ct = default)
    {
        DisposePlatformSubscription();
        CurrentMode = mode;

        var settings = new AppSettings { Id = AppSettings.SingletonId, ThemeMode = mode.ToString() };
        _ = await settingsRepository.SaveAsync(settings, ct).ConfigureAwait(false);

        ApplyMode(mode);
        _themeChanged.OnNext(mode);
        LogThemeChanged(logger, mode);

        return new Result<ThemeMode, ErrorResponse>.Ok(mode);
    }

    /// <inheritdoc />
    public void Dispose()
    {
        DisposePlatformSubscription();
        _themeChanged.Dispose();
    }

    private void ApplyMode(ThemeMode mode)
    {
        CurrentMode = mode;

        if (mode == ThemeMode.Auto)
        {
            SubscribeToPlatformChanges();
            return;
        }

        themeAdapter.Apply(mode == ThemeMode.Dark ? ThemeVariant.Dark : ThemeVariant.Light);
    }

    private void SubscribeToPlatformChanges()
    {
        themeAdapter.Apply(platformThemeProvider.IsDarkMode ? ThemeVariant.Dark : ThemeVariant.Light);

        _platformSubscription = platformThemeProvider.DarkModeChanged
            .ObserveOn(RxApp.MainThreadScheduler)
            .Subscribe(isDark => themeAdapter.Apply(isDark ? ThemeVariant.Dark : ThemeVariant.Light));
    }

    private void DisposePlatformSubscription()
    {
        _platformSubscription?.Dispose();
        _platformSubscription = null;
    }

    private static ThemeMode ParseMode(string? raw) =>
        Enum.TryParse<ThemeMode>(raw, out var mode) ? mode : ThemeMode.Auto;

    [LoggerMessage(Level = LogLevel.Debug, Message = "Theme initialised to {Mode}")]
    private static partial void LogThemeInitialised(MelILogger logger, ThemeMode mode);

    [LoggerMessage(Level = LogLevel.Debug, Message = "Theme changed to {Mode}")]
    private static partial void LogThemeChanged(MelILogger logger, ThemeMode mode);
}
