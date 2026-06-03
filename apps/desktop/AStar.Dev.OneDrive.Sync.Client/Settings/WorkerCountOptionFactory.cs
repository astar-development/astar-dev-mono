using AStar.Dev.OneDrive.Sync.Client.Localization;

namespace AStar.Dev.OneDrive.Sync.Client.Settings;

public static class WorkerCountOptionFactory
{
    /// <summary>Creates the localised list of WorkerCountOption instances.</summary>
    public static IReadOnlyList<WorkerCountOption> Create(ILocalizationService loc) =>
    [
        new(2, loc.GetLocal("Settings.ConcurrentWorkers.Count", 2)),
        new(4, loc.GetLocal("Settings.ConcurrentWorkers.Count", 4)),
        new(6, loc.GetLocal("Settings.ConcurrentWorkers.Count", 6)),
        new(8, loc.GetLocal("Settings.ConcurrentWorkers.Count", 8)),
    ];
}
