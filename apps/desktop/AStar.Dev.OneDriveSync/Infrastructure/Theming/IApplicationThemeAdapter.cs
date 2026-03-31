using Avalonia.Styling;

namespace AStar.Dev.OneDriveSync.Infrastructure.Theming;

/// <summary>Applies an Avalonia <see cref="ThemeVariant" /> to the running application.</summary>
public interface IApplicationThemeAdapter
{
    /// <summary>Sets <see cref="Avalonia.Application.RequestedThemeVariant" /> on the UI thread.</summary>
    void Apply(ThemeVariant variant);
}
