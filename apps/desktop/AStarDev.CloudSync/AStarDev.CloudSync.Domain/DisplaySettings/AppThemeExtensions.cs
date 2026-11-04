using AStarDev.CloudSync.Core.DisplaySettings;

namespace AStarDev.CloudSync.Domain.DisplaySettings;

/// <summary>Extensions for <see cref="AppTheme"/>.</summary>
public static class AppThemeExtensions
{
    /// <summary>The key used to persist the theme: "Dark" or "Light".</summary>
    /// <param name="theme">The theme to persist.</param>
    /// <returns>The persistence key, read back by <see cref="AppThemeFactory.FromKey"/>.</returns>
    public static string ToKey(this AppTheme theme) => theme is AppTheme.Light ? "Light" : "Dark";
}
