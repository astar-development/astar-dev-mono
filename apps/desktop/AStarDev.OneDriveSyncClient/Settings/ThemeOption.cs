using AStarDev.OneDriveSyncClient.Infrastructure.Theme;

namespace AStarDev.OneDriveSyncClient.Settings;

public sealed record ThemeOption(AppTheme Theme, string Label, bool IsSelected = false);
