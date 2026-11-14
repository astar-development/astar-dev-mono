using AStarDev.CloudSync.Accounts;
using AStarDev.CloudSync.Sync;
using AStarDev.FunctionalParadigm;

namespace AStarDev.CloudSync.TestHelpers.Common;

public static class AttentionItems
{
    public static SyncAttentionItem CreateConflict(AccountId accountId, string path) => CreateConflict(accountId, path, ConflictKindFactory.CreateBothModified());

    public static SyncAttentionItem CreateConflict(AccountId accountId, string path, ConflictKind kind) => CreateConflict(accountId, path, kind, Present(), Present());

    public static SyncAttentionItem CreateConflict(AccountId accountId, string path, ConflictKind kind, SideSnapshot local, SideSnapshot remote) => SyncAttentionItemFactory.CreateConflict(new ConflictId(Guid.NewGuid()), accountId, Path(path), kind, local, remote, TestClock.Now.AddHours(-30));

    public static SideSnapshot Present() => SideSnapshotFactory.CreatePresent(2_048, TestClock.Now.AddHours(-31), Option.None<ContentHash>(), Option.None<ContentTag>());

    public static RelativeSyncPath Path(string value) => RelativeSyncPathFactory.Create(value).Match(path => path, _ => throw new InvalidOperationException("The test path must be valid."));
}
