namespace AStarDev.WallpaperScraper.Startup;

/// <summary>
/// Collects failures recorded while running the application's startup steps, so the UI can surface
/// them instead of loading as if startup had succeeded.
/// </summary>
public sealed class StartupDiagnostics
{
    private readonly List<StartupFailure> failures = [];

    /// <summary>The startup failures recorded so far, in the order they occurred.</summary>
    public IReadOnlyList<StartupFailure> Failures => failures;

    /// <summary>Records a startup step failure.</summary>
    /// <param name="failure">The failure to record.</param>
    public void RecordFailure(StartupFailure failure) => failures.Add(failure);
}
