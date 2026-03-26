namespace AStar.Dev.OneDriveSync.old.Theming;

public sealed record ThemeOption(ThemeMode Mode, string DisplayName)
{
    public override string ToString() => DisplayName;
}
