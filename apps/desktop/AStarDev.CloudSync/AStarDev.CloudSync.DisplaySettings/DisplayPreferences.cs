namespace AStarDev.CloudSync.DisplaySettings;

/// <summary>The user's display choices, kept across restarts.</summary>
public sealed record DisplayPreferences
{
    internal DisplayPreferences(MainLayout layout, AppTheme theme)
    {
        Layout = layout;
        Theme = theme;
    }

    /// <summary>The main window layout.</summary>
    public MainLayout Layout { get; }

    /// <summary>The colour theme.</summary>
    public AppTheme Theme { get; }
}
