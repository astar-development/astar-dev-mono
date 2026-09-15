using AStarDev.Utilities;

namespace AStarDev.WallpaperScraper.Startup;

/// <summary>Creates <see cref="StartupFailure" /> instances, normalising invalid input.</summary>
public static class StartupFailureFactory
{
    private const string UnknownStep = "Unknown startup step";

    /// <summary>Creates a <see cref="StartupFailure" /> for the given step, normalising a blank step name.</summary>
    /// <param name="step">The name of the startup step that failed.</param>
    /// <param name="exception">The exception captured while running the failed startup step.</param>
    public static StartupFailure Create(string step, Exception exception) =>
        new(step.IsNullOrWhiteSpace() ? UnknownStep : step.Trim(), exception);
}
