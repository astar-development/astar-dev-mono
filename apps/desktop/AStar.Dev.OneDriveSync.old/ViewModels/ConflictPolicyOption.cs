using AStar.Dev.Conflict.Resolution;
using ReactiveUI;

namespace AStar.Dev.OneDriveSync.old.ViewModels;

public class ConflictPolicyOption : ReactiveObject
{
    public ConflictPolicy Policy { get; init; }
    public string Label { get; init; } = string.Empty;
    public string Description { get; init; } = string.Empty;
}
