using System.Diagnostics.CodeAnalysis;
using AStarDev.CloudSync.Sync;
using AStarDev.CloudSync.Tray;
using Avalonia.Controls;
using Avalonia.Threading;

namespace AStarDev.CloudSync.Notifications;

/// <inheritdoc />
/// <param name="visibility">Knows the main window.</param>
[ExcludeFromCodeCoverage(Justification = "A thin wrapper over the Avalonia window.")]
internal sealed class WindowPresenceReader(MainWindowVisibility visibility) : IWindowPresence
{
    /// <inheritdoc />
    public WindowPresence Current => Dispatcher.UIThread.CheckAccess() ? Read() : Dispatcher.UIThread.Invoke(Read);

    private WindowPresence Read() =>
        visibility.Window is { IsVisible: true, IsActive: true, WindowState: not WindowState.Minimized }
            ? WindowPresenceFactory.CreateInFront()
            : WindowPresenceFactory.CreateNotInFront();
}
