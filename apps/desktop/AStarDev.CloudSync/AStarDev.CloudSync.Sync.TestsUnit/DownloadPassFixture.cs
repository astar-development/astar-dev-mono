using System.IO.Abstractions;
using AStarDev.CloudSync.Accounts;
using AStarDev.CloudSync.TestHelpers.Accounts;
using AStarDev.CloudSync.TestHelpers.Common;
using AStarDev.FunctionalParadigm;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Time.Testing;
using Testably.Abstractions.Testing;

namespace AStarDev.CloudSync.Sync.TestsUnit;

public sealed class DownloadPassFixture : IDisposable
{
    public const string Root = "/sync/root";

    private static readonly DateTimeOffset Start = new(2026, 11, 13, 12, 0, 0, TimeSpan.Zero);

    private readonly ServiceProvider provider;

    private DownloadPassFixture(AccountId account, InMemoryAccountStore accounts, DownloadPassOptions? options)
    {
        Account = account;
        FileSystem = new MockFileSystem(options => options.SimulatingOperatingSystem(SimulationMode.Linux));
        Roots.Root = Option.Some(FileSystem.Directory.CreateDirectory(Root));
        Rules.Listing = Result.Success<IReadOnlyList<SelectionRule>, ErrorResponse>([SelectionTestRules.Include("Docs")]);
        provider = new ServiceCollection()
            .AddLogging(logging => logging.ClearProviders().AddProvider(Logs))
            .AddSingleton<System.TimeProvider>(Time)
            .AddSingleton<IAccountStore>(accounts)
            .AddSingleton<ISyncRootReader>(Roots)
            .AddSingleton<ISelectionRuleReader>(Rules)
            .AddSingleton<ISyncBaselineReader>(Baseline)
            .AddSingleton<ISyncBaselineWriter>(Baseline)
            .AddSingleton<ISyncAttentionWriter>(Attention)
            .AddSingleton<ISyncAttentionReader>(Attention)
            .AddSingleton<ISyncAttentionCloser>(Attention)
            .AddSingleton<IRemoteIndexReader>(Remote)
            .AddKeyedSingleton<IRemoteFileDownloader>(CloudProviderFactory.CreateOneDrive(), Downloader)
            .AddCloudSync()
            .AddSingleton<IRemoteIndexUpdater>(Steps)
            .AddSingleton<ILocalChangeDetector>(Steps)
            .AddSingleton<IUploadPass>(Steps)
            .AddSingleton<IPathQuarantine>(Quarantine)
            .AddSingleton<IDiskSpaceProbe>(Disk)
            .AddSingleton(options ?? DownloadPassOptionsFactory.Create())
            .BuildServiceProvider();
    }

    public AccountId Account { get; }

    public ScriptedSyncSteps Steps { get; } = new();

    public ISyncProgressFeed Feed => provider.GetRequiredService<ISyncProgressFeed>();

    public ISyncRunStatusSource Status => provider.GetRequiredService<ISyncRunStatusSource>();

    public Task<Result<SyncPassSummary, SyncPassFailure>> RunSyncAsync(CancellationToken cancellationToken) =>
        provider.GetRequiredService<ISyncPassRunner>().RunAsync(Account, SyncTriggerFactory.CreateManual(), cancellationToken);

    public MockFileSystem FileSystem { get; }

    public FakeTimeProvider Time { get; } = new(Start);

    public FixedSyncRootReader Roots { get; } = new();

    public ScriptedRuleReader Rules { get; } = new();

    public InMemorySyncBaseline Baseline { get; } = new();

    public RecordingAttentionWriter Attention { get; } = new();

    public ScriptedRemoteIndex Remote { get; } = new();

    public ScriptedDownloader Downloader { get; } = new();

    public FakeQuarantine Quarantine { get; } = new();

    public FakeDiskSpaceProbe Disk { get; } = new();

    public RecordingLoggerProvider Logs { get; } = new();

    public static async Task<DownloadPassFixture> CreateAsync(CancellationToken cancellationToken, DownloadPassOptions? options = null)
    {
        var accounts = new InMemoryAccountStore();
        var added = (AccountAddOutcome.Added)await accounts.AddAsync(AccountFixtures.Registration("00000000-0000-0000-a1b2-c3d4e5f60718.9188040d-6c67-4c5b-b112-36a304b66dad"), cancellationToken);

        return new DownloadPassFixture(added.AccountId, accounts, options);
    }

    public static RelativeSyncPath Path(string value) => RelativeSyncPathFactory.Create(value).Match(path => path, _ => throw new InvalidOperationException("The test path must be valid."));

    public IFileInfo Write(string relative, string content)
    {
        var full = $"{Root}/{relative}";
        _ = FileSystem.Directory.CreateDirectory(FileSystem.Path.GetDirectoryName(full)!);
        FileSystem.File.WriteAllText(full, content);
        FileSystem.File.SetLastWriteTimeUtc(full, Start.AddMinutes(-10).UtcDateTime);

        return FileSystem.FileInfo.New(full);
    }

    public SyncBaselineEntry Synced(string relative, string content, string itemId, string cTag = "c1")
    {
        var info = Write(relative, content);
        var hash = new Sha256ContentHasher().HashAsync(info, CancellationToken.None).GetAwaiter().GetResult().Match(value => value, _ => throw new InvalidOperationException("The test file must hash."));
        var entry = SyncBaselineEntryFactory.Create(Account, Path(relative), RemoteIds.Of(itemId), SyncBaselineKindFactory.CreateFile(info.Length, new DateTimeOffset(info.LastWriteTimeUtc, TimeSpan.Zero), hash, RemoteIds.CTag(cTag), RemoteIds.ETag("e-" + cTag)));
        Baseline.Add(entry);

        return entry;
    }

    public static DownloadPassSummary Summary(Result<DownloadPassSummary, DownloadPassFailure> result) => result.Match(summary => summary, failure => throw new InvalidOperationException($"The pass failed: {failure.GetType().Name}."));

    public SyncBaselineEntry SyncedFolder(string relative, string itemId)
    {
        _ = FileSystem.Directory.CreateDirectory($"{Root}/{relative}");
        var entry = SyncBaselineEntryFactory.Create(Account, Path(relative), RemoteIds.Of(itemId), SyncBaselineKindFactory.CreateFolder());
        Baseline.Add(entry);

        return entry;
    }

    public HeldDeletionId Hold(string pairPath, DeletionDirection direction, IEnumerable<string> paths)
    {
        var id = new HeldDeletionId(Guid.NewGuid());
        var held = paths.Select(Path).ToList();
        Attention.Seed(SyncAttentionItemFactory.CreateHeldDeletions(id, Account, SelectionTestRules.PairAt(pairPath), direction, held, Start));
        Quarantine.Snapshot = QuarantineSnapshotFactory.Extend(Quarantine.Snapshot, held.Select(path => KeyValuePair.Create(path, PathQuarantineStateFactory.CreateHeldByGuard())));

        return id;
    }

    public void Docs() => SyncedFolder("Docs", "remote-DOCS");

    public static LocalChangeSet Changes(params LocalChange[] changes) => LocalChangeSetFactory.Create(changes, 0, 0);

    public Task<Result<DownloadPassSummary, DownloadPassFailure>> RunAsync(LocalChangeSet changes, CancellationToken cancellationToken) => provider.GetRequiredService<IDownloadPass>().RunAsync(Account, changes, cancellationToken);

    public Task<Result<DownloadPassSummary, DownloadPassFailure>> RunAsync(CancellationToken cancellationToken) => RunAsync(Changes(), cancellationToken);

    public void Dispose()
    {
        provider.Dispose();
        Logs.Dispose();
    }
}
