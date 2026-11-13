using AStarDev.FunctionalParadigm;

namespace AStarDev.CloudSync.Sync;

/// <inheritdoc />
public sealed class ConflictClassifier : IConflictClassifier
{
    /// <inheritdoc />
    public Option<ConflictKind> Classify(ConflictEvidence evidence) => evidence.Signal switch
    {
        ConflictSignal.MoveTargetInUse => Option.Some(ConflictKindFactory.CreateMoveTargetTaken()),
        ConflictSignal.PreconditionFailed => Option.Some(ConflictKindFactory.CreateRemoteChangedDuringTransfer()),
        _ => evidence.Baseline.Match(baseline => AgainstBaseline(evidence, baseline), () => WithoutBaseline(evidence)),
    };

    private static Option<ConflictKind> WithoutBaseline(ConflictEvidence evidence) =>
        evidence.Local is SideSnapshot.Present && evidence.Remote is SideSnapshot.Present ? Option.Some(ConflictKindFactory.CreateBothAdded()) : Option.None<ConflictKind>();

    private static Option<ConflictKind> AgainstBaseline(ConflictEvidence evidence, ConflictBaseline baseline)
    {
        var localModified = evidence.Local is SideSnapshot.Present local && !SameHash(local, baseline);
        var remoteModified = evidence.Remote is SideSnapshot.Present remote && !SameTag(remote, baseline);

        if (localModified && remoteModified)
            return Option.Some(ConflictKindFactory.CreateBothModified());

        if (localModified && evidence.Remote is SideSnapshot.Absent)
            return Option.Some(ConflictKindFactory.CreateLocalModifiedRemoteDeleted());

        return evidence.Local is SideSnapshot.Absent && remoteModified ? Option.Some(ConflictKindFactory.CreateLocalDeletedRemoteModified()) : Option.None<ConflictKind>();
    }

    private static bool SameHash(SideSnapshot.Present side, ConflictBaseline baseline) => side.Hash.Match(hash => string.Equals(hash.Value, baseline.LocalHash.Value, StringComparison.Ordinal), () => false);

    private static bool SameTag(SideSnapshot.Present side, ConflictBaseline baseline) => side.Tag.Match(tag => string.Equals(tag.Value, baseline.RemoteTag.Value, StringComparison.Ordinal), () => false);
}
