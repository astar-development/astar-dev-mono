using AStarDev.FunctionalParadigm;

namespace AStarDev.CloudSync.Sync;

internal static class ConflictSides
{
    internal static SideSnapshot Local(Option<LocalChange> change) => change.Match(Local, SideSnapshotFactory.CreateAbsent);

    internal static SideSnapshot Local(LocalChange change) => change switch
    {
        LocalChange.Added added => SideSnapshotFactory.CreatePresent(added.Size, added.LastWriteUtc, Option.None<ContentHash>(), Option.None<ContentTag>()),
        LocalChange.Modified modified => SideSnapshotFactory.CreatePresent(modified.Size, modified.LastWriteUtc, Option.Some(modified.Hash), Option.None<ContentTag>()),
        _ => SideSnapshotFactory.CreateAbsent(),
    };

    internal static SideSnapshot Remote(Option<RemoteItemSnapshot> item) => item.Match(Remote, SideSnapshotFactory.CreateAbsent);

    internal static SideSnapshot Remote(RemoteItemSnapshot item) => item.Kind switch
    {
        RemoteItemKind.File file => SideSnapshotFactory.CreatePresent(file.Size, item.LastModified, file.Hash, Option.Some(file.CTag)),
        RemoteItemKind.Folder => SideSnapshotFactory.CreatePresent(0, item.LastModified, Option.None<ContentHash>(), Option.None<ContentTag>()),
        _ => SideSnapshotFactory.CreateAbsent(),
    };

    internal static ConflictFinding Finding(RelativeSyncPath path, ConflictKind kind, Option<LocalChange> local, Option<RemoteItemSnapshot> remote) => ConflictFindingFactory.Create(path, kind, Local(local), Remote(remote));
}
