using System.Diagnostics.CodeAnalysis;
using AStarDev.CloudSync.ViewModels.Logging;
using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Input.Platform;

namespace AStarDev.CloudSync.Logging;

/// <summary>Copies the viewer's lines to the system clipboard through the main window (#1411).</summary>
[ExcludeFromCodeCoverage(Justification = "A thin wrapper over the system clipboard, which CI cannot reach.")]
public sealed class AvaloniaLogDiagnosticsClipboard : ILogDiagnosticsClipboard
{
    /// <inheritdoc/>
    public async Task SetTextAsync(string text, CancellationToken cancellationToken)
    {
        if (Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime { MainWindow.Clipboard: { } clipboard })
            await clipboard.SetTextAsync(text);
    }
}
