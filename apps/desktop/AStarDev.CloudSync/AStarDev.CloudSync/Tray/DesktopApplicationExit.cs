using System.Diagnostics.CodeAnalysis;
using AStarDev.CloudSync.ViewModels.Tray;
using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Threading;

namespace AStarDev.CloudSync.Tray;

/// <inheritdoc />
[ExcludeFromCodeCoverage(Justification = "A thin wrapper over the Avalonia desktop lifetime.")]
internal sealed class DesktopApplicationExit : IApplicationExit
{
    /// <inheritdoc />
    public void Shutdown() => Dispatcher.UIThread.Post(() =>
    {
        if (Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
            desktop.Shutdown();
    });
}
