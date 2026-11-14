using AStarDev.CloudSync.Accounts;
using AStarDev.FunctionalParadigm;
using Microsoft.Extensions.DependencyInjection;
using static AStarDev.CloudSync.Sync.TestsUnit.SelectionTestRules;

namespace AStarDev.CloudSync.Sync.TestsUnit;

public sealed class DownloadScenario
{
    private static readonly DateTimeOffset Moment = new(2026, 11, 13, 9, 0, 0, TimeSpan.Zero);

    private readonly List<RemoteIndexEntry> remote = [];
    private readonly List<SyncBaselineEntry> baseline = [];
    private readonly List<LocalChange> local = [];
    private readonly List<SelectionRule> rules = [Include("Docs")];
    private readonly List<KeyValuePair<RelativeSyncPath, PathQuarantineState>> quarantine = [];
    private readonly List<RelativeSyncPath> missing = [];
    private QuarantineSnapshot? resyncing;
    private IConflictClassifier classifier = new ConflictClassifier();

    public static RelativeSyncPath At(string value) => new(value);

    public DownloadScenario Rules(params SelectionRule[] selection)
    {
        rules.Clear();
        rules.AddRange(selection);

        return this;
    }

    public DownloadScenario RemoteFile(string path, string id, string cTag, string? name = null)
    {
        remote.Add(RemoteIndexEntryFactory.Create(At(path), RemoteItemSnapshotFactory.Create(ItemId(id), Option.None<RemoteItemId>(), name ?? path[(path.LastIndexOf('/') + 1)..], RemoteItemKindFactory.CreateFile(10, Option.None<ContentHash>(), Tag(cTag)), Moment, ETag("e-" + cTag))));

        return this;
    }

    public DownloadScenario RemoteFolder(string path, string id)
    {
        remote.Add(RemoteIndexEntryFactory.Create(At(path), RemoteItemSnapshotFactory.Create(ItemId(id), Option.None<RemoteItemId>(), path[(path.LastIndexOf('/') + 1)..], RemoteItemKindFactory.CreateFolder(), Moment, ETag("e-folder"))));

        return this;
    }

    public DownloadScenario BaselineFile(string path, string id, string cTag)
    {
        baseline.Add(SyncBaselineEntryFactory.Create(Account, At(path), ItemId(id), SyncBaselineKindFactory.CreateFile(10, Moment, Hash("h-" + cTag), Tag(cTag), ETag("e-" + cTag))));

        return this;
    }

    public DownloadScenario BaselineFolder(string path, string id)
    {
        baseline.Add(SyncBaselineEntryFactory.Create(Account, At(path), ItemId(id), SyncBaselineKindFactory.CreateFolder()));

        return this;
    }

    public DownloadScenario LocalAdded(string path, bool folder = false)
    {
        local.Add(LocalChangeFactory.CreateAdded(At(path), folder ? LocalItemKindFactory.CreateFolder() : LocalItemKindFactory.CreateFile(), 5, Moment));

        return this;
    }

    public DownloadScenario LocalModified(string path)
    {
        local.Add(LocalChangeFactory.CreateModified(At(path), 5, Moment, Hash("local")));

        return this;
    }

    public DownloadScenario LocalDeleted(string path, string id)
    {
        local.Add(LocalChangeFactory.CreateDeleted(At(path), ItemId(id)));

        return this;
    }

    public DownloadScenario LocalDeferred(string path)
    {
        local.Add(LocalChangeFactory.CreateDeferred(At(path)));

        return this;
    }

    public DownloadScenario LocalUnreadable(string path)
    {
        local.Add(LocalChangeFactory.CreateUnreadable(At(path)));

        return this;
    }

    public DownloadScenario Quarantined(string path, PathQuarantineState state)
    {
        quarantine.Add(new KeyValuePair<RelativeSyncPath, PathQuarantineState>(At(path), state));

        return this;
    }

    public DownloadScenario ClassifiedBy(IConflictClassifier replacement)
    {
        classifier = replacement;

        return this;
    }

    public DownloadScenario Resyncing()
    {
        resyncing = QuarantineSnapshotFactory.CreateResyncing([]);

        return this;
    }

    public DownloadScenario MissingLocally(string path)
    {
        missing.Add(At(path));

        return this;
    }

    public DownloadPlan Plan()
    {
        using var provider = new ServiceCollection().AddCloudSync().AddSingleton(classifier).BuildServiceProvider();
        var snapshot = resyncing is null ? QuarantineSnapshotFactory.Create(quarantine) : QuarantineSnapshotFactory.Extend(resyncing, quarantine);

        return provider.GetRequiredService<IDownloadPlanner>().Plan(DownloadPlanRequestFactory.Create(remote, baseline, LocalChangeSetFactory.Create(local, 0, 0), rules, snapshot, missing));
    }

    public IReadOnlyList<string> Describe() => [.. Plan().Items.Select(Describe)];

    public static string Describe(DownloadPlanItem item) => item switch
    {
        DownloadPlanItem.DownloadFile download => $"download {download.Path.Value}",
        DownloadPlanItem.CreateLocalFolder folder => $"folder {folder.Path.Value}",
        DownloadPlanItem.MoveLocal move => $"move {move.From.Value} -> {move.To.Value}",
        DownloadPlanItem.DeleteLocal deletion => $"delete {deletion.Path.Value}",
        DownloadPlanItem.SendToConflicts conflict => $"conflict {conflict.Kind.GetType().Name} {conflict.Path.Value}",
        _ => "unknown",
    };

    private static RemoteItemId ItemId(string value) => RemoteItemIdFactory.Create(value).Match(id => id, _ => throw new InvalidOperationException("The test id must be valid."));

    private static ContentTag Tag(string value) => ContentTagFactory.Create(value).Match(tag => tag, _ => throw new InvalidOperationException("The test tag must be valid."));

    private static EntityTag ETag(string value) => EntityTagFactory.Create(value).Match(tag => tag, _ => throw new InvalidOperationException("The test tag must be valid."));

    private static ContentHash Hash(string value) => ContentHashFactory.Create(value).Match(hash => hash, _ => throw new InvalidOperationException("The test hash must be valid."));
}
