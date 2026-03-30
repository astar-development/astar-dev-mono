using AStar.Dev.Functional.Extensions;
using Microsoft.Extensions.Logging;

namespace AStar.Dev.OneDriveSync.Infrastructure.Theming;

/// <summary>Manages app-wide theming: DB persistence, OS observation, and runtime switching (AC TH-01 to TH-06).</summary>
public sealed partial class ThemeService : IThemeService, IDisposable
{
#pragma warning disable CS9113 // stub — parameters used once fully implemented
    public ThemeService(IAppSettingsRepository settingsRepository, IApplicationThemeAdapter themeAdapter, IPlatformThemeProvider platformThemeProvider, ILogger<ThemeService> logger) { }
#pragma warning restore CS9113

    public ThemeMode CurrentMode => throw new NotImplementedException();
    public IObservable<ThemeMode> ThemeChanged => throw new NotImplementedException();

    public Task<Result<ThemeMode, ErrorResponse>> InitialiseAsync(CancellationToken ct = default) => throw new NotImplementedException();
    public Task<Result<ThemeMode, ErrorResponse>> SetThemeAsync(ThemeMode mode, CancellationToken ct = default) => throw new NotImplementedException();

    public void Dispose() { }
}
