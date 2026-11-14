using AStarDev.CloudSync.Sync;

namespace AStarDev.CloudSync.ViewModels.Conflicts;

internal static class ConflictTexts
{
    internal const string RefusedKey = "Conflict.Refused";

    internal const string UnexpectedKey = "Conflict.Error.Unexpected";

    internal const string HeldNotSettledKey = "Held.Error.NotSettled";

    internal static string KindKey(ConflictKind kind) => kind switch
    {
        ConflictKind.BothModified => "Conflict.Kind.BothModified",
        ConflictKind.LocalModifiedRemoteDeleted => "Conflict.Kind.LocalModifiedRemoteDeleted",
        ConflictKind.LocalDeletedRemoteModified => "Conflict.Kind.LocalDeletedRemoteModified",
        ConflictKind.BothAdded => "Conflict.Kind.BothAdded",
        ConflictKind.MoveTargetTaken => "Conflict.Kind.MoveTargetTaken",
        _ => "Conflict.Kind.RemoteChangedDuringTransfer",
    };

    internal static string StrategyKey(ConflictStrategy strategy) => strategy switch
    {
        ConflictStrategy.LocalWins => "Conflict.KeepLocal",
        ConflictStrategy.RemoteWins => "Conflict.KeepCloud",
        ConflictStrategy.KeepBoth => "Conflict.KeepBoth",
        _ => "Conflict.Skip",
    };

    internal static string NotAllowedKey(ResolutionNotAllowed reason) => reason switch
    {
        ResolutionNotAllowed.LocalSideAbsent => "Conflict.Unavailable.LocalSideAbsent",
        ResolutionNotAllowed.RemoteSideAbsent => "Conflict.Unavailable.RemoteSideAbsent",
        _ => "Conflict.Unavailable.NotAConflict",
    };

    internal static string FailureKey(ResolutionFailure failure) => failure switch
    {
        ResolutionFailure.NotAllowed => "Conflict.Error.NotAllowed",
        ResolutionFailure.NotOpen => "Conflict.Error.NotOpen",
        ResolutionFailure.Unsupported => "Conflict.Error.Unsupported",
        ResolutionFailure.LocalFileMissing => "Conflict.Error.LocalFileMissing",
        ResolutionFailure.RemoteFileMissing => "Conflict.Error.RemoteFileMissing",
        ResolutionFailure.LocalChanged => "Conflict.Error.LocalChanged",
        ResolutionFailure.RemoteChanged => "Conflict.Error.RemoteChanged",
        ResolutionFailure.Transfer => "Conflict.Error.Transfer",
        ResolutionFailure.Local => "Conflict.Error.Local",
        ResolutionFailure.NotRecorded => "Conflict.Error.NotRecorded",
        _ => UnexpectedKey,
    };
}
