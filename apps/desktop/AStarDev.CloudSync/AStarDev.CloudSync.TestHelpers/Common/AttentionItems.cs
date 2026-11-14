using AStarDev.CloudSync.Accounts;
using AStarDev.CloudSync.Sync;
using AStarDev.FunctionalParadigm;

namespace AStarDev.CloudSync.TestHelpers.Common;

public static class AttentionItems
{
    public static SyncAttentionItem CreateConflict(AccountId accountId, string path) => CreateConflict(accountId, path, ConflictKindFactory.CreateBothModified());

    public static SyncAttentionItem CreateConflict(AccountId accountId, string path, ConflictKind kind) => CreateConflict(accountId, path, kind, Present(), Present());

    public static SyncAttentionItem CreateConflict(AccountId accountId, string path, ConflictKind kind, SideSnapshot local, SideSnapshot remote) => SyncAttentionItemFactory.CreateConflict(new ConflictId(Guid.NewGuid()), accountId, Path(path), kind, local, remote, TestClock.Now.AddHours(-30));

    public static CloudAccount Account(string name) => CloudAccountFactory.Create(CloudAccountIdentityFactory.Create(AccountIdFactory.Create(Guid.NewGuid()), name, $"{name.ToLowerInvariant()}@example.com", CloudProviderFactory.CreateOneDrive()), StorageUsageFactory.Create(1_048_576, 1_073_741_824), 1, 1, SyncStatusFactory.CreateUpToDate(), Option.None<DateTimeOffset>());

    public static SideSnapshot Present() => SideSnapshotFactory.CreatePresent(2_048, TestClock.Now.AddHours(-31), Option.None<ContentHash>(), Option.None<ContentTag>());

    public static RelativeSyncPath Path(string value) => RelativeSyncPathFactory.Create(value).Match(path => path, _ => throw new InvalidOperationException("The test path must be valid."));

    public static SyncAttentionItem CreateHeld(AccountId accountId, SyncPairId pairId, DeletionDirection direction, params string[] paths) => SyncAttentionItemFactory.CreateHeldDeletions(new HeldDeletionId(Guid.NewGuid()), accountId, pairId, direction, [.. paths.Select(Path)], TestClock.Now.AddHours(-2));

    public static SyncPair Pair(AccountId accountId, SyncPairId pairId, string name) => SyncPairFactory.Create(pairId, accountId, name, RelativePaths.Of(name), Option.None<RemoteItemId>(), SyncDirectionFactory.CreateTwoWay(), SyncStatusFactory.CreateUpToDate(), 0, Option.None<DateTimeOffset>());
}
