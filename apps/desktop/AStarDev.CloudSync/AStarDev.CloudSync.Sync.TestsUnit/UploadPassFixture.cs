using AStarDev.CloudSync.Accounts;
using AStarDev.CloudSync.TestHelpers.Accounts;
using AStarDev.CloudSync.TestHelpers.Common;
using AStarDev.FunctionalParadigm;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Testably.Abstractions.Testing;
using static AStarDev.CloudSync.Sync.TestsUnit.SelectionTestRules;

namespace AStarDev.CloudSync.Sync.TestsUnit;

public sealed class UploadPassFixture : IDisposable
{
    private static readonly DateTimeOffset Written = new(2026, 11, 10, 9, 0, 0, TimeSpan.Zero);

    private readonly List<LocalChange> changes = [];
    private ServiceProvider? provider;

    public MockFileSystem FileSystem { get; } = new(options => options.SimulatingOperatingSystem(SimulationMode.Linux));

    public InMemorySyncBaseline Baseline { get; } = new();

    public Microsoft.Extensions.Time.Testing.FakeTimeProvider Time { get; } = new(new DateTimeOffset(2026, 11, 13, 12, 0, 0, TimeSpan.Zero));

    public ISyncProgressFeed Feed => provider!.GetRequiredService<ISyncProgressFeed>();

    public RemoteTreeIndex Remote { get; } = new();

    public FakeUploader Uploader { get; } = new();

    public FakeFolderCreator Folders { get; } = new();

    public FakeItemRemover Remover { get; } = new();

    public FakeRemoteNamePolicy Names { get; } = new();

    public FakeUploadRecorder Recorder { get; }

    public RecordingLoggerProvider Logs { get; } = new();

    public IReadOnlyList<SelectionRule> Rules { get; set; } = [Include("Docs")];

    public QuarantineSnapshot Quarantine { get; set; } = QuarantineSnapshotFactory.CreateEmpty();

    public IConflictClassifier Classifier { get; set; } = new ConflictClassifier();

    public bool HasRoot { get; set; } = true;

    public int MaxConcurrentUploads { get; set; } = 8;

    public AccountId Account { get; private set; }

    public UploadPassFixture() => Recorder = new FakeUploadRecorder(Baseline);

    public UploadPassFixture AddedFile(string path, long size = 10) => Local(LocalChangeFactory.CreateAdded(UploadPlanFixture.At(path), LocalItemKindFactory.CreateFile(), size, Written));

    public UploadPassFixture AddedFolder(string path) => Local(LocalChangeFactory.CreateAdded(UploadPlanFixture.At(path), LocalItemKindFactory.CreateFolder(), 0, Written));

    public UploadPassFixture ModifiedFile(string path) => Local(LocalChangeFactory.CreateModified(UploadPlanFixture.At(path), 20, Written, UploadPlanFixture.Hash("new")));

    public UploadPassFixture DeletedItem(string path, string itemId) => Local(LocalChangeFactory.CreateDeleted(UploadPlanFixture.At(path), UploadPlanFixture.Id(itemId)));

    public UploadPassFixture BaselineFile(string path, string itemId, string cTag = "c1", string eTag = "e1")
    {
        Baseline.Add(SyncBaselineEntryFactory.Create(SelectionTestRules.Account, UploadPlanFixture.At(path), UploadPlanFixture.Id(itemId), SyncBaselineKindFactory.CreateFile(10, Written, UploadPlanFixture.Hash("old"), new ContentTag(cTag), new EntityTag(eTag))));

        return this;
    }

    public UploadPassFixture BaselineFolder(string path, string itemId)
    {
        Baseline.Add(SyncBaselineEntryFactory.Create(SelectionTestRules.Account, UploadPlanFixture.At(path), UploadPlanFixture.Id(itemId), SyncBaselineKindFactory.CreateFolder()));

        return this;
    }

    public async Task<UploadPassFixture> BuildAsync(CancellationToken cancellationToken)
    {
        var accounts = new InMemoryAccountStore();
        Account = ((AccountAddOutcome.Added)await accounts.AddAsync(AccountFixtures.Registration("00000000-0000-0000-a1b2-c3d4e5f60718.9188040d-6c67-4c5b-b112-36a304b66dad"), cancellationToken)).AccountId;
        _ = FileSystem.Directory.CreateDirectory("/sync");
        var oneDrive = CloudProviderFactory.CreateOneDrive();
        var roots = new FixedSyncRootReader { Root = HasRoot ? Option.Some(FileSystem.DirectoryInfo.New("/sync")) : Option.None<System.IO.Abstractions.IDirectoryInfo>() };
        provider = new ServiceCollection()
            .AddLogging(logging => logging.ClearProviders().AddProvider(Logs))
            .AddSingleton<System.TimeProvider>(Time)
            .AddCloudSync()
            .AddSingleton(Classifier)
            .AddSingleton<IAccountStore>(accounts)
            .AddSingleton<ISyncRootReader>(roots)
            .AddSingleton<ISelectionRuleReader>(new FixedSelectionRuleReader(Rules))
            .AddSingleton<IPathQuarantine>(new FixedPathQuarantine(Quarantine))
            .AddSingleton<ISyncBaselineReader>(Baseline)
            .AddSingleton<ISyncBaselineWriter>(Baseline)
            .AddSingleton<IRemoteIndexReader>(Remote)
            .AddSingleton<IUploadRecorder>(Recorder)
            .AddSingleton(UploadPassOptionsFactory.Create(MaxConcurrentUploads))
            .AddKeyedSingleton<IRemoteFileUploader>(oneDrive, Uploader)
            .AddKeyedSingleton<IRemoteFolderCreator>(oneDrive, Folders)
            .AddKeyedSingleton<IRemoteItemRemover>(oneDrive, Remover)
            .AddKeyedSingleton<IRemoteNamePolicy>(oneDrive, Names)
            .BuildServiceProvider();

        return this;
    }

    public Task<Result<UploadPassSummary, UploadPassFailure>> RunAsync(CancellationToken cancellationToken) =>
        provider!.GetRequiredService<IUploadPass>().RunAsync(Account, LocalChangeSetFactory.Create(changes, changes.Count, 0), cancellationToken);

    public void Dispose() => provider?.Dispose();

    private UploadPassFixture Local(LocalChange change)
    {
        changes.Add(change);

        return this;
    }
}
