namespace AStarDev.CloudSync.ViewModels.Logging;

/// <summary>Puts the log viewer's lines on the clipboard (#1411).</summary>
public interface ILogDiagnosticsClipboard
{
    /// <summary>Replaces the clipboard text.</summary>
    /// <param name="text">The text to copy.</param>
    /// <param name="cancellationToken">Cancels the copy.</param>
    Task SetTextAsync(string text, CancellationToken cancellationToken);
}
