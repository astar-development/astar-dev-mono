namespace AStar.Dev.OneDriveSync.Theming;

public sealed record ThemeOption(ThemeMode Mode, string DisplayName)
{
    public override string ToString() => DisplayName;
}
