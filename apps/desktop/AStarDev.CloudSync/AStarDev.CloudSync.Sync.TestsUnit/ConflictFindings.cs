using AStarDev.FunctionalParadigm;

namespace AStarDev.CloudSync.Sync.TestsUnit;

public static class ConflictFindings
{
    public static ConflictFinding Of(string path, ConflictKind kind) => ConflictFindingFactory.Create(new RelativeSyncPath(path), kind, Present(), Present());

    public static ConflictFinding Of(RelativeSyncPath path, ConflictKind kind) => ConflictFindingFactory.Create(path, kind, Present(), Present());

    public static SideSnapshot Present() => SideSnapshotFactory.CreatePresent(10, new DateTimeOffset(2026, 11, 14, 9, 0, 0, TimeSpan.Zero), Option.None<ContentHash>(), Option.None<ContentTag>());
}
