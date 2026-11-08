namespace AStarDev.CloudSync.DisplaySettings;

/// <summary>Applies an <see cref="AppTheme"/> to the running application.</summary>
public interface IThemeApplier
{
    /// <summary>Applies the theme immediately.</summary>
    /// <param name="theme">The theme to apply.</param>
    void Apply(AppTheme theme);
}
