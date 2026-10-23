using System.Diagnostics.CodeAnalysis;
using Avalonia.Controls;

namespace AStarDev.OneDriveSyncClient.Infrastructure.Notifications;

/// <inheritdoc />
[ExcludeFromCodeCoverage]
public sealed class AvaloniaAppActivityProvider : IAppActivityProvider
{
    private Window? mainWindow;

    /// <summary>Tracks <paramref name="window"/> to decide whether the application is in the foreground.</summary>
    public void Attach(Window window) => mainWindow = window;

    /// <inheritdoc />
    public AppActivity Current() => mainWindow is { IsVisible: true, IsActive: true } ? AppActivityFactory.CreateForeground() : AppActivityFactory.CreateBackground();
}
