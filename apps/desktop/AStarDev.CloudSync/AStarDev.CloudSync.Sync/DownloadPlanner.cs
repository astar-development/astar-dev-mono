using AStarDev.FunctionalParadigm;

namespace AStarDev.CloudSync.Sync;

/// <inheritdoc />
/// <param name="evaluator">Judges paths against the selection rules.</param>
/// <param name="classifier">Decides the conflict kind when a remote move lands on a taken local path.</param>
internal sealed class DownloadPlanner(ISyncSelectionEvaluator evaluator, IConflictClassifier classifier) : IDownloadPlanner
{
    /// <inheritdoc />
    public DownloadPlan Plan(DownloadPlanRequest request)
    {
        var state = new DownloadPlanState(request, evaluator);
        var moves = FolderMoves(state);
        var items = state.Remote.SelectMany(entry => ForRemote(state, entry, moves)).Concat(state.Baseline.SelectMany(entry => ForBaseline(state, entry)));

        return Guard(state, OnePerPath(items));
    }

    private static IEnumerable<DownloadPlanItem> Conflict(RelativeSyncPath path, ConflictKind kind, Option<LocalChange> local, Option<RemoteItemSnapshot> remote) => [DownloadPlanItemFactory.CreateSendToConflicts(ConflictSides.Finding(path, kind, local, remote), remote)];

    private static IEnumerable<DownloadPlanItem> Fetch(RemoteIndexEntry entry) =>
        entry.Item.Kind is RemoteItemKind.File ? [DownloadPlanItemFactory.CreateDownloadFile(entry.Path, entry.Item)] : [DownloadPlanItemFactory.CreateCreateLocalFolder(entry.Path, entry.Item.Id)];

    private static bool IsFolder(RemoteIndexEntry entry) => entry.Item.Kind is RemoteItemKind.Folder;

    private static bool IsHeldBack(PathQuarantineState state) => state is PathQuarantineState.Conflicted or PathQuarantineState.HeldByGuard or PathQuarantineState.Skipped;

    private static bool WaitsForNextPass(LocalChange change) => change is LocalChange.Deferred or LocalChange.Unreadable;

    private static bool ChangesData(LocalChange change) => change is LocalChange.Added or LocalChange.Modified or LocalChange.Deleted;

    private static bool IsCoveredBy(DownloadFolderMove move, RelativeSyncPath from, RelativeSyncPath to) =>
        DownloadPlanState.IsBelow(from, move.From) && string.Equals(to.Value, move.To.Value + from.Value[move.From.Value.Length..], StringComparison.OrdinalIgnoreCase);

    private static int Phase(DownloadPlanItem item) => item switch
    {
        DownloadPlanItem.CreateLocalFolder => 0,
        DownloadPlanItem.MoveLocal => 1,
        DownloadPlanItem.DownloadFile => 2,
        DownloadPlanItem.SendToConflicts => 3,
        _ => 4,
    };

    private static IReadOnlyList<DownloadPlanItem> OnePerPath(IEnumerable<DownloadPlanItem> items) =>
        [.. items
            .GroupBy(item => item.Path)
            .Select(group => group.First())
            .OrderBy(Phase)
            .ThenBy(item => item is DownloadPlanItem.DeleteLocal ? -DownloadPlanState.Depth(item.Path) : DownloadPlanState.Depth(item.Path))
            .ThenBy(item => item.Path.Value, StringComparer.OrdinalIgnoreCase)];

    private static IEnumerable<DownloadPlanItem> New(DownloadPlanState state, RemoteIndexEntry entry) =>
        state.LocalAt(entry.Path).Match<IEnumerable<DownloadPlanItem>>(change => NewOverLocal(entry, change), () => state.BaselineAt(entry.Path).Match<IEnumerable<DownloadPlanItem>>(stale => Replace(entry, stale), () => Fetch(entry)));

    private static IEnumerable<DownloadPlanItem> NewOverLocal(RemoteIndexEntry entry, LocalChange change) => change switch
    {
        LocalChange.Added added when IsFolder(entry) && added.Kind is LocalItemKind.Folder => Fetch(entry),
        LocalChange.Added => Conflict(change.Path, ConflictKindFactory.CreateBothAdded(), Option.Some(change), Option.Some(entry.Item)),
        LocalChange.Modified => Conflict(entry.Path, ConflictKindFactory.CreateBothModified(), Option.Some(change), Option.Some(entry.Item)),
        LocalChange.Deleted => Conflict(entry.Path, ConflictKindFactory.CreateLocalDeletedRemoteModified(), Option.Some(change), Option.Some(entry.Item)),
        _ => [],
    };

    private static IEnumerable<DownloadPlanItem> Replace(RemoteIndexEntry entry, SyncBaselineEntry stale) =>
        IsFolder(entry) == stale.Kind is SyncBaselineKind.Folder ? Fetch(entry) : Conflict(entry.Path, ConflictKindFactory.CreateBothModified(), Option.None<LocalChange>(), Option.Some(entry.Item));

    private static IEnumerable<DownloadFolderMove> FolderMoves(DownloadPlanState state) =>
        state.Remote.Where(IsFolder).SelectMany(entry => state.BaselineOf(entry.Item.Id).Match<IEnumerable<DownloadFolderMove>>(baseline => MoveOf(state, baseline, entry), () => []));

    private static IEnumerable<DownloadFolderMove> MoveOf(DownloadPlanState state, SyncBaselineEntry baseline, RemoteIndexEntry entry) =>
        baseline.Path == entry.Path ? [] : [new DownloadFolderMove(baseline.Path, entry.Path, IsBlocked(state, baseline.Path))];

    private static bool IsBlocked(DownloadPlanState state, RelativeSyncPath folder) =>
        state.LocalAt(folder).Match(WaitsForNextPassOrChanges, () => false) || state.LocalBelow(folder).Any(change => WaitsForNextPass(change) || ChangesData(change));

    private static bool WaitsForNextPassOrChanges(LocalChange change) => WaitsForNextPass(change) || ChangesData(change);

    private IEnumerable<DownloadPlanItem> ForRemote(DownloadPlanState state, RemoteIndexEntry entry, IEnumerable<DownloadFolderMove> moves)
    {
        if (!state.Downloads(entry.Path) || IsHeldBack(state.QuarantineAt(entry.Path)))
            return [];

        return state.BaselineOf(entry.Item.Id).Match(baseline => baseline.Path == entry.Path ? Unchanged(state, entry, baseline) : Moved(state, entry, baseline, [.. moves]), () => New(state, entry));
    }

    private static IEnumerable<DownloadPlanItem> Unchanged(DownloadPlanState state, RemoteIndexEntry entry, SyncBaselineEntry baseline) => (entry.Item.Kind, baseline.Kind) switch
    {
        (RemoteItemKind.File remote, SyncBaselineKind.File synced) => UnchangedFile(state, entry, remote.CTag != synced.CTag),
        (RemoteItemKind.Folder, SyncBaselineKind.Folder) => [],
        _ => Conflict(entry.Path, ConflictKindFactory.CreateBothModified(), state.LocalAt(entry.Path), Option.Some(entry.Item)),
    };

    private static IEnumerable<DownloadPlanItem> UnchangedFile(DownloadPlanState state, RemoteIndexEntry entry, bool remoteChanged) =>
        state.LocalAt(entry.Path).Match(
            change => change switch
            {
                LocalChange.Modified when remoteChanged => Conflict(entry.Path, ConflictKindFactory.CreateBothModified(), Option.Some(change), Option.Some(entry.Item)),
                LocalChange.Deleted when remoteChanged => Conflict(entry.Path, ConflictKindFactory.CreateLocalDeletedRemoteModified(), Option.Some(change), Option.Some(entry.Item)),
                _ => [],
            },
            () => remoteChanged || (state.IsMissingLocally(entry.Path) && state.QuarantineAt(entry.Path) is PathQuarantineState.Clear) ? Fetch(entry) : []);

    private IEnumerable<DownloadPlanItem> Moved(DownloadPlanState state, RemoteIndexEntry entry, SyncBaselineEntry baseline, IReadOnlyList<DownloadFolderMove> moves)
    {
        var from = baseline.Path;
        if (IsHeldBack(state.QuarantineAt(from)) || moves.Any(move => move.Blocked && DownloadPlanState.IsBelow(from, move.From)))
            return [];

        var below = baseline.Kind is SyncBaselineKind.Folder ? state.LocalBelow(from).ToList() : [];
        var own = state.LocalAt(from);
        if (own.Match(WaitsForNextPass, () => false) || below.Any(WaitsForNextPass))
            return [];

        return own.Match(change => change is LocalChange.Deleted ? Conflict(from, ConflictKindFactory.CreateLocalDeletedRemoteModified(), own, Option.Some(entry.Item)) : Conflict(from, ConflictKindFactory.CreateBothModified(), own, Option.Some(entry.Item)), () => MovedUnchangedItself(state, entry, baseline, below, moves));
    }

    private IEnumerable<DownloadPlanItem> MovedUnchangedItself(DownloadPlanState state, RemoteIndexEntry entry, SyncBaselineEntry baseline, IReadOnlyList<LocalChange> below, IReadOnlyList<DownloadFolderMove> moves)
    {
        if (below.Any(ChangesData))
            return Conflict(baseline.Path, ConflictKindFactory.CreateBothModified(), state.LocalAt(baseline.Path), Option.Some(entry.Item));

        if (IsTargetTaken(state, entry, baseline))
            return Conflict(entry.Path, MoveTargetKind(state, entry), state.LocalAt(entry.Path), Option.Some(entry.Item));

        return moves.Any(move => !move.Blocked && IsCoveredBy(move, baseline.Path, entry.Path)) ? [] : [DownloadPlanItemFactory.CreateMoveLocal(baseline.Path, entry.Path, entry.Item.Id)];
    }

    private ConflictKind MoveTargetKind(DownloadPlanState state, RemoteIndexEntry entry)
    {
        var evidence = ConflictEvidenceFactory.Create(ConflictSides.Local(state.LocalAt(entry.Path)), ConflictSides.Remote(entry.Item), Option.None<ConflictBaseline>(), ConflictSignalFactory.CreateMoveTargetInUse());

        return classifier.Classify(evidence).Match(kind => kind, ConflictKindFactory.CreateMoveTargetTaken);
    }

    private static bool IsTargetTaken(DownloadPlanState state, RemoteIndexEntry entry, SyncBaselineEntry baseline) =>
        state.LocalAt(entry.Path).Match(change => change is LocalChange.Added or LocalChange.Modified, () => false) || state.BaselineAt(entry.Path).Match(other => other.ItemId != baseline.ItemId, () => false);

    private static IEnumerable<DownloadPlanItem> ForBaseline(DownloadPlanState state, SyncBaselineEntry baseline)
    {
        if (!state.Downloads(baseline.Path) || state.QuarantineAt(baseline.Path) is not PathQuarantineState.Clear || IsStillSelected(state, baseline) || state.RemoteAt(baseline.Path).Match(_ => true, () => false))
            return [];

        return state.LocalAt(baseline.Path).Match(change => change is LocalChange.Modified ? Conflict(baseline.Path, ConflictKindFactory.CreateLocalModifiedRemoteDeleted(), Option.Some(change), Option.None<RemoteItemSnapshot>()) : [], () => DeleteUnchanged(state, baseline));
    }

    private static bool IsStillSelected(DownloadPlanState state, SyncBaselineEntry baseline) => state.RemoteOf(baseline.ItemId).Match(remote => state.Downloads(remote.Path), () => false);

    private static IEnumerable<DownloadPlanItem> DeleteUnchanged(DownloadPlanState state, SyncBaselineEntry baseline) =>
        baseline.Kind is SyncBaselineKind.Folder && state.LocalBelow(baseline.Path).Any(change => WaitsForNextPass(change) || change is LocalChange.Added or LocalChange.Modified)
            ? []
            : [DownloadPlanItemFactory.CreateDeleteLocal(baseline.Path, baseline.ItemId)];

    private static DownloadPlan Guard(DownloadPlanState state, IReadOnlyList<DownloadPlanItem> items)
    {
        var totals = state.Baseline.Select(entry => state.PairOf(entry.Path)).SelectMany(pair => pair.Match(found => new[] { found }, () => [])).GroupBy(pair => pair).ToDictionary(group => group.Key, group => group.Count());
        var held = items
            .OfType<DownloadPlanItem.DeleteLocal>()
            .GroupBy(deletion => state.PairOf(deletion.Path).Match(pair => pair, () => default))
            .Where(group => MassDeletionGuard.Evaluate(totals.GetValueOrDefault(group.Key), group.Count()) is MassDeletionVerdict.Held)
            .Select(group => HeldLocalDeletionsFactory.Create(group.Key, group.Select(deletion => deletion.Path), totals.GetValueOrDefault(group.Key)))
            .ToList();
        var heldPaths = held.SelectMany(pair => pair.Paths).ToHashSet();

        return DownloadPlanFactory.Create(items.Where(item => item is not DownloadPlanItem.DeleteLocal || !heldPaths.Contains(item.Path)), held);
    }
}
