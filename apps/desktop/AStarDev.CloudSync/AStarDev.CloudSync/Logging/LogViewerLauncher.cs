using System.Diagnostics.CodeAnalysis;
using AStarDev.CloudSync.ViewModels.Logging;

namespace AStarDev.CloudSync.Logging;

/// <summary>Opens the log viewer, once: asking again brings the open one forward (#1411).</summary>
/// <param name="viewModel">The viewer's view model.</param>
[ExcludeFromCodeCoverage(Justification = "Opens an Avalonia window, which CI cannot show; the viewer logic is in LogViewerViewModel.")]
public sealed class LogViewerLauncher(LogViewerViewModel viewModel)
{
    private LogViewerWindow? window;

    /// <summary>Shows the viewer with freshly read files, or activates it when it is already open.</summary>
    public void Open()
    {
        if (window is not null)
        {
            window.Activate();

            return;
        }

        window = new LogViewerWindow { DataContext = viewModel };
        window.Closed += (_, _) => window = null;
        window.Show();
        _ = viewModel.RefreshAsync(CancellationToken.None);
    }
}
