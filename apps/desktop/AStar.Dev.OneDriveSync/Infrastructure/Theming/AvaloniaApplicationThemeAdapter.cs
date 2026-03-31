using Avalonia;
using Avalonia.Styling;
using Avalonia.Threading;

namespace AStar.Dev.OneDriveSync.Infrastructure.Theming;

/// <summary>Sets <see cref="Application.RequestedThemeVariant" /> on the Avalonia UI thread.</summary>
internal sealed class AvaloniaApplicationThemeAdapter : IApplicationThemeAdapter
{
    /// <inheritdoc />
    public void Apply(ThemeVariant variant) =>
        Dispatcher.UIThread.Post(() =>
        {
            if (Application.Current is { } app)
                app.RequestedThemeVariant = variant;
        });
}
