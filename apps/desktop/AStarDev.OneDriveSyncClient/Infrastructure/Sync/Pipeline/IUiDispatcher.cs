namespace AStarDev.OneDriveSyncClient.Infrastructure.Sync.Pipeline;

/// <summary>
/// Abstracts posting work onto the UI thread, allowing non-Avalonia tests to supply a synchronous pass-through.
/// </summary>
public interface IUiDispatcher
{
    /// <summary>Posts <paramref name="action"/> onto the UI thread.</summary>
    void Post(Action action);
}
