namespace AStarDev.CloudSync.Core;

/// <summary>The user's display choices, kept across restarts.</summary>
/// <param name="Layout">The main window layout.</param>
/// <param name="Theme">The colour theme.</param>
public sealed record DisplayPreferences(MainLayout Layout, AppTheme Theme);
