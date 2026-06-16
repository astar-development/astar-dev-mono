using AStar.Dev.OneDrive.Sync.Client.Data.Entities;
using AStar.Dev.OneDrive.Sync.Client.Localization;

namespace AStar.Dev.OneDrive.Sync.Client.Conflicts;

public static class ConflictPolicyOptionFactory
{
    /// <summary>Creates the localised list of ConflictPolicyOption instances with <see cref="ConflictPolicyOption.IsSelected"/> set for the matching policy.</summary>
    public static IReadOnlyList<ConflictPolicyOption> Create(ILocalizationService loc, ConflictPolicy selectedPolicy) =>
    [
        new(ConflictPolicy.Ignore,        loc.GetLocal("ConflictPolicy.Ignore"),        loc.GetLocal("ConflictPolicy.Ignore.Description"),        selectedPolicy == ConflictPolicy.Ignore),
        new(ConflictPolicy.KeepBoth,      loc.GetLocal("ConflictPolicy.KeepBoth"),      loc.GetLocal("ConflictPolicy.KeepBoth.Description"),      selectedPolicy == ConflictPolicy.KeepBoth),
        new(ConflictPolicy.LastWriteWins, loc.GetLocal("ConflictPolicy.LastWriteWins"), loc.GetLocal("ConflictPolicy.LastWriteWins.Description"), selectedPolicy == ConflictPolicy.LastWriteWins),
        new(ConflictPolicy.LocalWins,     loc.GetLocal("ConflictPolicy.LocalWins"),     loc.GetLocal("ConflictPolicy.LocalWins.Description"),     selectedPolicy == ConflictPolicy.LocalWins),
        new(ConflictPolicy.RemoteWins,    loc.GetLocal("ConflictPolicy.RemoteWins"),    loc.GetLocal("ConflictPolicy.RemoteWins.Description"),    selectedPolicy == ConflictPolicy.RemoteWins),
    ];
}
