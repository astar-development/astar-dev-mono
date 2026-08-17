namespace AStarDev.OneDriveSyncClient.Settings;

public sealed record SyncIntervalOption(int Minutes, string Label, bool IsSelected = false);
