using AStarDev.OneDriveSyncClient.Localization;

namespace AStarDev.OneDriveSyncClient.Settings;

public static class WorkerCountOptionFactory
{
    /// <summary>Creates the localised list of WorkerCountOption instances with <see cref="WorkerCountOption.IsSelected"/> set for the matching count.</summary>
    public static IReadOnlyList<WorkerCountOption> Create(ILocalizationService loc, int selectedCount) =>
    [
        new(2, loc.GetLocal("Settings.ConcurrentWorkers.Count", 2), selectedCount == 2),
        new(4, loc.GetLocal("Settings.ConcurrentWorkers.Count", 4), selectedCount == 4),
        new(6, loc.GetLocal("Settings.ConcurrentWorkers.Count", 6), selectedCount == 6),
        new(8, loc.GetLocal("Settings.ConcurrentWorkers.Count", 8), selectedCount == 8),
    ];
}
