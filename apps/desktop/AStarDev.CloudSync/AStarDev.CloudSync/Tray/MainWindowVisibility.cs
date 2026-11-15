using System.Diagnostics.CodeAnalysis;
using AStarDev.CloudSync.ViewModels.Tray;
using Avalonia.Controls;
using Avalonia.Threading;

namespace AStarDev.CloudSync.Tray;

/// <inheritdoc />
[ExcludeFromCodeCoverage(Justification = "A thin wrapper over the Avalonia window.")]
internal sealed class MainWindowVisibility : IMainWindowVisibility
{
    private Window? window;

    /// <summary>Tells the service which window to show and hide.</summary>
    /// <param name="mainWindow">The main window.</param>
    internal void Attach(Window mainWindow) => window = mainWindow;

    /// <summary>The main window, once it has been attached.</summary>
    internal Window? Window => window;

    /// <inheritdoc />
    public void Show() => Dispatcher.UIThread.Post(() =>
    {
        if (window is null)
            return;

        window.Show();
        if (window.WindowState == WindowState.Minimized)
            window.WindowState = WindowState.Normal;

        window.Activate();
    });

    /// <inheritdoc />
    public void Hide() => Dispatcher.UIThread.Post(() => window?.Hide());

    /// <inheritdoc />
    public void Minimise() => Dispatcher.UIThread.Post(() =>
    {
        if (window is not null)
            window.WindowState = WindowState.Minimized;
    });
}
