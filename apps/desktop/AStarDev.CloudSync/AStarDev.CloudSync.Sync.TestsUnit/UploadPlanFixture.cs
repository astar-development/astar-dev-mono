using AStarDev.CloudSync.Accounts;
using AStarDev.FunctionalParadigm;
using Microsoft.Extensions.DependencyInjection;
using static AStarDev.CloudSync.Sync.TestsUnit.SelectionTestRules;

namespace AStarDev.CloudSync.Sync.TestsUnit;

public sealed class UploadPlanFixture : IDisposable
{
    private static readonly DateTimeOffset Written = new(2026, 11, 10, 9, 0, 0, TimeSpan.Zero);

    private readonly ServiceProvider provider = new ServiceCollection().AddCloudSync().BuildServiceProvider();
    private readonly List<LocalChange> changes = [];
    private readonly Dictionary<RelativeSyncPath, SyncBaselineEntry> baseline = [];
    private readonly Dictionary<RelativeSyncPath, RemoteItemSnapshot> remote = [];

    public FakeRemoteNamePolicy Names { get; } = new();

    public IReadOnlyList<SelectionRule> Rules { get; set; } = [Include("Docs")];

    public static RelativeSyncPath At(string value) => RelativeSyncPathFactory.Create(value).Match(path => path, _ => throw new InvalidOperationException("The test path must be valid."));

    public static RemoteItemId Id(string value) => RemoteItemIdFactory.Create(value).Match(id => id, _ => throw new InvalidOperationException("The test id must be valid."));

    public static ContentHash Hash(string value) => ContentHashFactory.Create(value).Match(hash => hash, _ => throw new InvalidOperationException("The test hash must be valid."));

    public UploadPlanFixture AddedFile(string path, long size = 10) => Local(LocalChangeFactory.CreateAdded(At(path), LocalItemKindFactory.CreateFile(), size, Written));

    public UploadPlanFixture AddedFolder(string path) => Local(LocalChangeFactory.CreateAdded(At(path), LocalItemKindFactory.CreateFolder(), 0, Written));

    public UploadPlanFixture ModifiedFile(string path, long size = 20) => Local(LocalChangeFactory.CreateModified(At(path), size, Written, Hash("new-hash")));

    public UploadPlanFixture DeletedItem(string path, string itemId) => Local(LocalChangeFactory.CreateDeleted(At(path), Id(itemId)));

    public UploadPlanFixture DeferredFile(string path) => Local(LocalChangeFactory.CreateDeferred(At(path)));

    public UploadPlanFixture UnreadableFile(string path) => Local(LocalChangeFactory.CreateUnreadable(At(path)));

    public UploadPlanFixture BaselineFile(string path, string itemId, string cTag = "c1", string eTag = "e1") =>
        Baseline(path, itemId, SyncBaselineKindFactory.CreateFile(10, Written, Hash("old-hash"), new ContentTag(cTag), new EntityTag(eTag)));

    public UploadPlanFixture BaselineFolder(string path, string itemId) => Baseline(path, itemId, SyncBaselineKindFactory.CreateFolder());

    public UploadPlanFixture RemoteFile(string path, string itemId, string cTag = "c1", string eTag = "e1") =>
        Remote(path, RemoteItemSnapshotFactory.Create(Id(itemId), Option.None<RemoteItemId>(), path.Split('/')[^1], RemoteItemKindFactory.CreateFile(10, Option.None<ContentHash>(), new ContentTag(cTag)), Written, new EntityTag(eTag)));

    public UploadPlanFixture RemoteFolder(string path, string itemId, string eTag = "fe1") =>
        Remote(path, RemoteItemSnapshotFactory.Create(Id(itemId), Option.None<RemoteItemId>(), path.Split('/')[^1], RemoteItemKindFactory.CreateFolder(), Written, new EntityTag(eTag)));

    public UploadPlanFixture RemoteDeleted(string path, string itemId) =>
        Remote(path, RemoteItemSnapshotFactory.Create(Id(itemId), Option.None<RemoteItemId>(), path.Split('/')[^1], RemoteItemKindFactory.CreateDeleted(), Written, new EntityTag("gone")));

    public UploadPlan Plan() => provider.GetRequiredService<IUploadPlanner>().Plan(LocalChangeSetFactory.Create(changes, changes.Count, 0), baseline, remote, Rules, Names);

    public void Dispose() => provider.Dispose();

    private UploadPlanFixture Local(LocalChange change)
    {
        changes.Add(change);

        return this;
    }

    private UploadPlanFixture Baseline(string path, string itemId, SyncBaselineKind kind)
    {
        baseline[At(path)] = SyncBaselineEntryFactory.Create(Account, At(path), Id(itemId), kind);

        return this;
    }

    private UploadPlanFixture Remote(string path, RemoteItemSnapshot snapshot)
    {
        remote[At(path)] = snapshot;

        return this;
    }
}
