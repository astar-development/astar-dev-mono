using AStarDev.FunctionalParadigm;

namespace AStarDev.CloudSync.Sync;

/// <inheritdoc />
/// <param name="evaluator">Tells which paths are in an upload pair.</param>
internal sealed class UploadPlanner(ISyncSelectionEvaluator evaluator) : IUploadPlanner
{
    private const char Separator = '/';
    private const int FolderGroup = 0;
    private const int FileGroup = 1;
    private const int RemovalGroup = 2;
    private const int ReportedGroup = 3;

    /// <inheritdoc />
    public UploadPlan Plan(LocalChangeSet changes, IReadOnlyDictionary<RelativeSyncPath, SyncBaselineEntry> baseline, IReadOnlyDictionary<RelativeSyncPath, RemoteItemSnapshot> remote, IReadOnlyList<SelectionRule> rules, IRemoteNamePolicy names)
    {
        var present = remote.Where(pair => pair.Value.Kind is not RemoteItemKind.Deleted).ToDictionary(pair => pair.Key, pair => pair.Value);
        var items = changes.Changes
            .Where(change => IsUploadable(change, baseline, rules))
            .SelectMany(change => PlanOne(change, baseline, present, names).Match(item => new[] { item }, Array.Empty<UploadPlanItem>))
            .OrderBy(GroupOf)
            .ThenBy(item => GroupOf(item) == RemovalGroup ? -Depth(item.Path) : Depth(item.Path))
            .ThenBy(item => item.Path.Value, StringComparer.OrdinalIgnoreCase);

        return UploadPlanFactory.Create(items);
    }

    private static int GroupOf(UploadPlanItem item) => item switch
    {
        UploadPlanItem.EnsureRemoteFolder or UploadPlanItem.LinkExisting => FolderGroup,
        UploadPlanItem.UploadNewFile or UploadPlanItem.UploadReplace => FileGroup,
        UploadPlanItem.RemoveRemote or UploadPlanItem.ForgetBaseline => RemovalGroup,
        _ => ReportedGroup,
    };

    private static int Depth(RelativeSyncPath path) => (path.Value ?? string.Empty).Count(character => character == Separator);

    private static string NameOf(RelativeSyncPath path)
    {
        var value = path.Value ?? string.Empty;

        return value[(value.LastIndexOf(Separator) + 1)..];
    }

    private static bool IsBelow(RelativeSyncPath path, RelativeSyncPath folder) => (path.Value ?? string.Empty).StartsWith((folder.Value ?? string.Empty) + Separator, StringComparison.OrdinalIgnoreCase);

    private static Option<UploadPlanItem> Some(UploadPlanItem item) => Option.Some(item);

    private static Option<UploadPlanItem> Conflict(LocalChange change, ConflictKind kind, Option<RemoteItemSnapshot> remote) => Some(UploadPlanItemFactory.CreateSendToConflicts(ConflictSides.Finding(change.Path, kind, Option.Some(change), remote), remote));

    private static Option<RemoteItemSnapshot> RemoteAt(IReadOnlyDictionary<RelativeSyncPath, RemoteItemSnapshot> remote, RelativeSyncPath path) => remote.TryGetValue(path, out var found) ? Option.Some(found) : Option.None<RemoteItemSnapshot>();

    private static Option<SyncBaselineEntry> BaselineAt(IReadOnlyDictionary<RelativeSyncPath, SyncBaselineEntry> baseline, RelativeSyncPath path) => baseline.TryGetValue(path, out var found) ? Option.Some(found) : Option.None<SyncBaselineEntry>();

    private bool IsUploadable(LocalChange change, IReadOnlyDictionary<RelativeSyncPath, SyncBaselineEntry> baseline, IReadOnlyList<SelectionRule> rules)
    {
        if (change is LocalChange.Deferred or LocalChange.Unreadable)
            return false;

        var isFolder = change is LocalChange.Added { Kind: LocalItemKind.Folder } || (change is LocalChange.Deleted && BaselineAt(baseline, change.Path).Match(entry => entry.Kind is SyncBaselineKind.Folder, () => false));
        var folder = isFolder ? change.Path.Value : ParentOf(change.Path);

        return LocalRelativePathFactory.Create(folder).Match(path => evaluator.IsIncluded(path, rules) && evaluator.EnclosingDirection(path, rules).Match(direction => direction is not SyncDirection.DownloadOnly, () => false), _ => false);
    }

    private static string ParentOf(RelativeSyncPath path)
    {
        var value = path.Value ?? string.Empty;
        var cut = value.LastIndexOf(Separator);

        return cut < 0 ? string.Empty : value[..cut];
    }

    private static Option<UploadPlanItem> PlanOne(LocalChange change, IReadOnlyDictionary<RelativeSyncPath, SyncBaselineEntry> baseline, IReadOnlyDictionary<RelativeSyncPath, RemoteItemSnapshot> remote, IRemoteNamePolicy names) => change switch
    {
        LocalChange.Added added => PlanAdded(added, remote, names),
        LocalChange.Modified modified => PlanModified(modified, baseline, remote, names),
        LocalChange.Deleted deleted => PlanDeleted(deleted, baseline, remote),
        _ => Option.None<UploadPlanItem>(),
    };

    private static Option<UploadPlanItem> PlanAdded(LocalChange.Added added, IReadOnlyDictionary<RelativeSyncPath, RemoteItemSnapshot> remote, IRemoteNamePolicy names) =>
        names.Check(NameOf(added.Path), added.Path, added.Kind is LocalItemKind.Folder ? 0 : added.Size).Match(
            _ => RemoteAt(remote, added.Path).Match(existing => AddedOverExisting(added, existing), () => Some(added.Kind is LocalItemKind.Folder ? UploadPlanItemFactory.CreateEnsureRemoteFolder(added.Path) : UploadPlanItemFactory.CreateUploadNewFile(added.Path, added.Size, added.LastWriteUtc))),
            reason => Some(UploadPlanItemFactory.CreateRejected(added.Path, reason)));

    private static Option<UploadPlanItem> AddedOverExisting(LocalChange.Added added, RemoteItemSnapshot existing) =>
        added.Kind is LocalItemKind.Folder && existing.Kind is RemoteItemKind.Folder
            ? Some(UploadPlanItemFactory.CreateLinkExisting(added.Path, existing.Id))
            : Conflict(added, ConflictKindFactory.CreateBothAdded(), Option.Some(existing));

    private static Option<UploadPlanItem> PlanModified(LocalChange.Modified modified, IReadOnlyDictionary<RelativeSyncPath, SyncBaselineEntry> baseline, IReadOnlyDictionary<RelativeSyncPath, RemoteItemSnapshot> remote, IRemoteNamePolicy names) =>
        names.Check(NameOf(modified.Path), modified.Path, modified.Size).Match(
            _ => BaselineAt(baseline, modified.Path).Match(entry => ModifiedAgainstBaseline(modified, entry, RemoteAt(remote, modified.Path)), () => Conflict(modified, ConflictKindFactory.CreateBothAdded(), RemoteAt(remote, modified.Path))),
            reason => Some(UploadPlanItemFactory.CreateRejected(modified.Path, reason)));

    private static Option<UploadPlanItem> ModifiedAgainstBaseline(LocalChange.Modified modified, SyncBaselineEntry entry, Option<RemoteItemSnapshot> remote) =>
        remote.Match(
            snapshot => entry.Kind is SyncBaselineKind.File file && snapshot.Kind is RemoteItemKind.File current && snapshot.Id == entry.ItemId && current.CTag == file.CTag
                ? Replace(modified, snapshot, file.CTag)
                : Conflict(modified, ConflictKindFactory.CreateBothModified(), remote),
            () => Conflict(modified, ConflictKindFactory.CreateLocalModifiedRemoteDeleted(), remote));

    private static Option<UploadPlanItem> Replace(LocalChange.Modified modified, RemoteItemSnapshot snapshot, ContentTag cTag) =>
        UploadPreconditionFactory.CreateMustMatch(cTag).Match(
            precondition => Some(UploadPlanItemFactory.CreateUploadReplace(modified.Path, snapshot.Id, modified.Size, modified.LastWriteUtc, precondition)),
            _ => Conflict(modified, ConflictKindFactory.CreateBothModified(), Option.Some(snapshot)));

    private static Option<UploadPlanItem> PlanDeleted(LocalChange.Deleted deleted, IReadOnlyDictionary<RelativeSyncPath, SyncBaselineEntry> baseline, IReadOnlyDictionary<RelativeSyncPath, RemoteItemSnapshot> remote) =>
        BaselineAt(baseline, deleted.Path).Match(
            entry => RemoteAt(remote, deleted.Path).Match(snapshot => DeletedAgainstRemote(deleted, entry, snapshot, baseline, remote), () => Some(UploadPlanItemFactory.CreateForgetBaseline(deleted.Path))),
            () => Some(UploadPlanItemFactory.CreateForgetBaseline(deleted.Path)));

    private static Option<UploadPlanItem> DeletedAgainstRemote(LocalChange.Deleted deleted, SyncBaselineEntry entry, RemoteItemSnapshot snapshot, IReadOnlyDictionary<RelativeSyncPath, SyncBaselineEntry> baseline, IReadOnlyDictionary<RelativeSyncPath, RemoteItemSnapshot> remote)
    {
        var conflict = Conflict(deleted, ConflictKindFactory.CreateLocalDeletedRemoteModified(), Option.Some(snapshot));
        if (snapshot.Id != entry.ItemId)
            return conflict;

        return entry.Kind switch
        {
            SyncBaselineKind.File file => snapshot.Kind is RemoteItemKind.File current && current.CTag == file.CTag ? RemoveByContent(deleted, snapshot.Id, file.CTag, conflict) : conflict,
            _ => snapshot.Kind is RemoteItemKind.Folder && !HasUnseenChange(deleted.Path, baseline, remote) ? Remove(deleted, snapshot.Id, snapshot.ETag, conflict) : conflict,
        };
    }

    private static Option<UploadPlanItem> RemoveByContent(LocalChange.Deleted deleted, RemoteItemId itemId, ContentTag cTag, Option<UploadPlanItem> conflict) =>
        RemovePreconditionFactory.CreateMustMatchContent(cTag).Match(precondition => Some(UploadPlanItemFactory.CreateRemoveRemote(deleted.Path, itemId, precondition)), _ => conflict);

    private static Option<UploadPlanItem> Remove(LocalChange.Deleted deleted, RemoteItemId itemId, EntityTag eTag, Option<UploadPlanItem> conflict) =>
        RemovePreconditionFactory.CreateMustMatch(eTag).Match(precondition => Some(UploadPlanItemFactory.CreateRemoveRemote(deleted.Path, itemId, precondition)), _ => conflict);

    private static bool HasUnseenChange(RelativeSyncPath folder, IReadOnlyDictionary<RelativeSyncPath, SyncBaselineEntry> baseline, IReadOnlyDictionary<RelativeSyncPath, RemoteItemSnapshot> remote) =>
        remote.Where(pair => IsBelow(pair.Key, folder)).Any(pair => !IsSeen(pair.Value, BaselineAt(baseline, pair.Key)));

    private static bool IsSeen(RemoteItemSnapshot snapshot, Option<SyncBaselineEntry> entry) =>
        entry.Match(known => known.ItemId == snapshot.Id && (known.Kind, snapshot.Kind) switch
        {
            (SyncBaselineKind.File file, RemoteItemKind.File current) => file.CTag == current.CTag,
            (SyncBaselineKind.Folder, RemoteItemKind.Folder) => true,
            _ => false,
        }, () => false);
}
