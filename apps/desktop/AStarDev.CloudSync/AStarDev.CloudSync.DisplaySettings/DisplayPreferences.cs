namespace AStarDev.CloudSync.DisplaySettings;

/// <summary>The user's display choices, kept across restarts.</summary>
public sealed record DisplayPreferences
{
    internal DisplayPreferences(MainLayout layout, AppTheme theme, string locale)
    {
        Layout = layout;
        Theme = theme;
        Locale = locale;
    }

    /// <summary>The main window layout.</summary>
    public MainLayout Layout { get; }

    /// <summary>The colour theme.</summary>
    public AppTheme Theme { get; }

    /// <summary>The UI locale, for example <c>en-GB</c>. Only <c>en-GB</c> exists today; the choice is kept so a later locale is a data change (#1413).</summary>
    public string Locale { get; }
}
