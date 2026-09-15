namespace AStarDev.WallpaperScraper.Startup;

/// <summary>Represents a single startup step that failed, and why.</summary>
public record StartupFailure
{
    internal StartupFailure(string step, Exception exception)
    {
        Step = step;
        Exception = exception;
    }

    /// <summary>The name of the startup step that failed, e.g. "Database migration".</summary>
    public string Step { get; }

    /// <summary>The exception captured while running the failed startup step.</summary>
    public Exception Exception { get; }
}
