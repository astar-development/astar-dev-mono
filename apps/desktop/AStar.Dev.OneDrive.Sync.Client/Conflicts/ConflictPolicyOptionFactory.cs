using AStar.Dev.OneDrive.Sync.Client.Data.Entities;
using AStar.Dev.OneDrive.Sync.Client.Localization;

namespace AStar.Dev.OneDrive.Sync.Client.Conflicts;

public static class ConflictPolicyOptionFactory
{
    /// <summary>Creates the localised list of ConflictPolicyOption instances.</summary>
    public static IReadOnlyList<ConflictPolicyOption> Create(ILocalizationService loc) =>
    [
        new(ConflictPolicy.Ignore,        loc.GetLocal("ConflictPolicy.Ignore"),        loc.GetLocal("ConflictPolicy.Ignore.Description")),
        new(ConflictPolicy.KeepBoth,      loc.GetLocal("ConflictPolicy.KeepBoth"),      loc.GetLocal("ConflictPolicy.KeepBoth.Description")),
        new(ConflictPolicy.LastWriteWins, loc.GetLocal("ConflictPolicy.LastWriteWins"), loc.GetLocal("ConflictPolicy.LastWriteWins.Description")),
        new(ConflictPolicy.LocalWins,     loc.GetLocal("ConflictPolicy.LocalWins"),     loc.GetLocal("ConflictPolicy.LocalWins.Description")),
        new(ConflictPolicy.RemoteWins,    loc.GetLocal("ConflictPolicy.RemoteWins"),    loc.GetLocal("ConflictPolicy.RemoteWins.Description")),
    ];
}
