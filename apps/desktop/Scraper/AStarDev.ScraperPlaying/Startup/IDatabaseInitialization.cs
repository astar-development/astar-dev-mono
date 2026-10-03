namespace AStarDev.ScraperPlaying.Startup;

/// <summary>The background preparation of the application database.</summary>
public interface IDatabaseInitialization
{
    /// <summary>Starts the preparation on first call and returns the task that completes when it has finished, or faults if it failed.</summary>
    Task ReadyAsync();
}
