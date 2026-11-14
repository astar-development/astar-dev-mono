using AStarDev.FunctionalParadigm;

namespace AStarDev.CloudSync.Sync;

internal sealed class DownloadPlanState
{
    private const char Separator = '/';

    private readonly ISyncSelectionEvaluator evaluator;
    private readonly Dictionary<RelativeSyncPath, LocalChange> local;
    private readonly Dictionary<RelativeSyncPath, SyncBaselineEntry> baselineByPath;
    private readonly Dictionary<RemoteItemId, SyncBaselineEntry> baselineById;
    private readonly Dictionary<RelativeSyncPath, RemoteIndexEntry> remoteByPath;
    private readonly Dictionary<RemoteItemId, RemoteIndexEntry> remoteById;
    private readonly HashSet<RelativeSyncPath> missing;

    internal DownloadPlanState(DownloadPlanRequest request, ISyncSelectionEvaluator evaluator)
    {
        this.evaluator = evaluator;
        Request = request;
        local = request.Local.Changes.ToDictionary(change => change.Path);
        baselineByPath = request.Baseline.GroupBy(entry => entry.Path).ToDictionary(group => group.Key, group => group.First());
        baselineById = request.Baseline.GroupBy(entry => entry.ItemId).ToDictionary(group => group.Key, group => group.First());
        Remote = [.. request.Remote.Where(entry => entry.Item.Kind is not RemoteItemKind.Deleted && !LocalTrash.Contains(entry.Path)).GroupBy(entry => entry.Item.Id).Select(group => group.OrderBy(entry => entry.Path.Value, StringComparer.OrdinalIgnoreCase).First())];
        remoteById = Remote.ToDictionary(entry => entry.Item.Id);
        remoteByPath = Remote.GroupBy(entry => entry.Path).ToDictionary(group => group.Key, group => group.First());
        missing = request.MissingLocally.ToHashSet();
    }

    internal DownloadPlanRequest Request { get; }

    internal IReadOnlyList<RemoteIndexEntry> Remote { get; }

    internal IEnumerable<SyncBaselineEntry> Baseline => baselineById.Values;

    internal static bool IsBelow(RelativeSyncPath path, RelativeSyncPath ancestor) => path.Value.Length > ancestor.Value.Length && path.Value.StartsWith(ancestor.Value + Separator, StringComparison.OrdinalIgnoreCase);

    internal static int Depth(RelativeSyncPath path) => path.Value.Count(character => character == Separator);

    internal bool Downloads(RelativeSyncPath path) => evaluator.Evaluate(new LocalRelativePath(path.Value), Request.Rules) is SelectionState.Included included && included.Direction is not SyncDirection.UploadOnly;

    internal Option<SyncPairId> PairOf(RelativeSyncPath path) => evaluator.Evaluate(new LocalRelativePath(path.Value), Request.Rules) is SelectionState.Included included ? Option.Some(included.Pair) : Option.None<SyncPairId>();

    internal PathQuarantineState QuarantineAt(RelativeSyncPath path) => Request.Quarantine.StateOf(path);

    internal Option<LocalChange> LocalAt(RelativeSyncPath path) => local.TryGetValue(path, out var change) ? Option.Some(change) : Option.None<LocalChange>();

    internal IEnumerable<LocalChange> LocalBelow(RelativeSyncPath ancestor) => local.Values.Where(change => IsBelow(change.Path, ancestor));

    internal Option<SyncBaselineEntry> BaselineAt(RelativeSyncPath path) => baselineByPath.TryGetValue(path, out var entry) ? Option.Some(entry) : Option.None<SyncBaselineEntry>();

    internal Option<SyncBaselineEntry> BaselineOf(RemoteItemId itemId) => baselineById.TryGetValue(itemId, out var entry) && Downloads(entry.Path) ? Option.Some(entry) : Option.None<SyncBaselineEntry>();

    internal Option<RemoteIndexEntry> RemoteOf(RemoteItemId itemId) => remoteById.TryGetValue(itemId, out var entry) ? Option.Some(entry) : Option.None<RemoteIndexEntry>();

    internal Option<RemoteIndexEntry> RemoteAt(RelativeSyncPath path) => remoteByPath.TryGetValue(path, out var entry) ? Option.Some(entry) : Option.None<RemoteIndexEntry>();

    internal bool IsMissingLocally(RelativeSyncPath path) => missing.Contains(path);
}
