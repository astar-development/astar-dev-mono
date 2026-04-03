using System;

namespace AStar.Dev.OneDriveSync.Infrastructure.Theming;

/// <summary>Abstracts OS-level dark/light theme detection for testability.</summary>
public interface IPlatformThemeProvider
{
    /// <summary>Returns <see langword="true" /> when the OS is currently in dark mode.</summary>
    bool IsDarkMode { get; }

    /// <summary>Emits <see langword="true" /> when the OS switches to dark mode, <see langword="false" /> for light.</summary>
    IObservable<bool> DarkModeChanged { get; }
}
