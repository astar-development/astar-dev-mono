using Avalonia;
using Avalonia.Styling;

namespace AStar.Dev.OneDriveSync.Theming;

public sealed class AvaloniaApplicationThemeAdapter : IApplicationThemeAdapter
{
    public void SetThemeVariant(ThemeVariant variant)
        => Application.Current!.RequestedThemeVariant = variant;
}
