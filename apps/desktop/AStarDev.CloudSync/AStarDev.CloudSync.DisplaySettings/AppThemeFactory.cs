
namespace AStarDev.CloudSync.DisplaySettings;

/// <summary>Creates <see cref="AppTheme"/> cases.</summary>
public static class AppThemeFactory
{
    /// <summary>Creates the dark theme.</summary>
    public static AppTheme CreateDark() => new AppTheme.Dark();

    /// <summary>Creates the light theme.</summary>
    public static AppTheme CreateLight() => new AppTheme.Light();

    /// <summary>Creates the theme a persisted key names; an unknown or missing key gives the default, dark.</summary>
    /// <param name="key">The persisted key, as produced by <see cref="AppThemeExtensions.ToKey"/>.</param>
    public static AppTheme FromKey(string? key) => string.Equals(key?.Trim(), "Light", StringComparison.OrdinalIgnoreCase) ? CreateLight() : CreateDark();
}
