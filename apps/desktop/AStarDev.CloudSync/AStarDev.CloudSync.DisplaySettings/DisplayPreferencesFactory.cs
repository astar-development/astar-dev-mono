namespace AStarDev.CloudSync.DisplaySettings;

/// <summary>Creates <see cref="DisplayPreferences"/>.</summary>
public static class DisplayPreferencesFactory
{
    /// <summary>The locale used when none, or an unsupported one, is given.</summary>
    public const string DefaultLocale = "en-GB";

    private static readonly string[] SupportedLocales = [DefaultLocale];

    /// <summary>Creates preferences for the given layout and theme, in the default locale.</summary>
    /// <param name="layout">The main window layout.</param>
    /// <param name="theme">The colour theme.</param>
    public static DisplayPreferences Create(MainLayout layout, AppTheme theme) => Create(layout, theme, DefaultLocale);

    /// <summary>Creates preferences for the given layout, theme and locale. A blank or unsupported locale becomes the default; a supported one is given its standard casing.</summary>
    /// <param name="layout">The main window layout.</param>
    /// <param name="theme">The colour theme.</param>
    /// <param name="locale">The UI locale, for example <c>en-GB</c>.</param>
    public static DisplayPreferences Create(MainLayout layout, AppTheme theme, string? locale) => new(layout, theme, SupportedLocales.FirstOrDefault(supported => string.Equals(supported, locale?.Trim(), StringComparison.OrdinalIgnoreCase)) ?? DefaultLocale);

    /// <summary>Creates the defaults: layout A, the dark theme and <c>en-GB</c>.</summary>
    public static DisplayPreferences CreateDefault() => Create(MainLayoutFactory.CreateUnifiedAccounts(), AppThemeFactory.CreateDark());
}
