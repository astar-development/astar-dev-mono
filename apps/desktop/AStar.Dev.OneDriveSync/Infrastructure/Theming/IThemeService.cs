using AStar.Dev.Functional.Extensions;

namespace AStar.Dev.OneDriveSync.Infrastructure.Theming;

/// <summary>Manages the app-wide theme: persistence, OS observation, and runtime switching.</summary>
public interface IThemeService
{
    /// <summary>The theme mode currently active.</summary>
    ThemeMode CurrentMode { get; }

    /// <summary>Emits every time the active theme mode changes.</summary>
    IObservable<ThemeMode> ThemeChanged { get; }

    /// <summary>
    ///     Loads the stored theme from the database and applies it.
    ///     Falls back to <see cref="ThemeMode.Auto" /> when no record exists or the DB is unavailable.
    /// </summary>
    Task<Result<ThemeMode, ErrorResponse>> InitialiseAsync(CancellationToken ct = default);

    /// <summary>Persists <paramref name="mode" />, applies it immediately, and notifies subscribers.</summary>
    Task<Result<ThemeMode, ErrorResponse>> SetThemeAsync(ThemeMode mode, CancellationToken ct = default);
}
