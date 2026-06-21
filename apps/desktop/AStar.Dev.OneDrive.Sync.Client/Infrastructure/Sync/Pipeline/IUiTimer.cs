namespace AStar.Dev.OneDrive.Sync.Client.Infrastructure.Sync.Pipeline;

/// <summary>
/// Abstracts a repeating UI-thread timer, allowing non-Avalonia tests to supply a manually-driven stub.
/// </summary>
public interface IUiTimer
{
    /// <summary>Starts the timer, invoking <paramref name="callback"/> on the UI thread every <paramref name="interval"/>.</summary>
    void Start(TimeSpan interval, Action callback);
}
