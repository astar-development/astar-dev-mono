using AStarDev.CloudSync.Core.DisplaySettings;
using Avalonia;
using Avalonia.Styling;

namespace AStarDev.CloudSync.Theming;

/// <summary>Applies an <see cref="AppTheme"/> by setting the application's requested theme variant; the CloudSync tokens follow it.</summary>
public sealed class AvaloniaThemeApplier : IThemeApplier
{
    /// <inheritdoc />
    public void Apply(AppTheme theme)
    {
        if (Application.Current is null)
            return;

        Application.Current.RequestedThemeVariant = theme is AppTheme.Light ? ThemeVariant.Light : ThemeVariant.Dark;
    }
}
