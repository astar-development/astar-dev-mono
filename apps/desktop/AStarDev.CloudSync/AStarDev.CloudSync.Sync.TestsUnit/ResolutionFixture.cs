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

public sealed class ResolutionFixture : IDisposable
{
    public const string Root = "/sync/root";

    public static readonly DateTimeOffset Start = new(2026, 11, 14, 9, 5, 7, TimeSpan.Zero);

    private readonly ServiceProvider provider;

    private ResolutionFixture(AccountId account, InMemoryAccountStore accounts)
    {
        Account = account;
        FileSystem = new MockFileSystem(options => options.SimulatingOperatingSystem(SimulationMode.Linux));
        Roots.Root = Option.Some(FileSystem.Directory.CreateDirectory(Root));
        Recorder = new FakeUploadRecorder(Baseline);
        TrashRetention.Current = Result.Success<LocalTrashOptions, ErrorResponse>(LocalTrashOptionsFactory.Create());
        var oneDrive = CloudProviderFactory.CreateOneDrive();
        provider = new ServiceCollection()
            .AddLogging(logging => logging.ClearProviders().AddProvider(Logs))
            .AddSingleton<System.TimeProvider>(Time)
            .AddCloudSync()
            .AddSingleton<IAccountStore>(accounts)
            .AddSingleton<ISyncRootReader>(Roots)
            .AddSingleton<ISelectionRuleReader>(new FixedSelectionRuleReader([]))
            .AddSingleton<IPathQuarantine>(new FixedPathQuarantine(QuarantineSnapshotFactory.CreateEmpty()))
            .AddSingleton<ISyncBaselineReader>(Baseline)
            .AddSingleton<ISyncBaselineWriter>(Baseline)
            .AddSingleton<ISyncAttentionWriter>(Attention)
            .AddSingleton<ISyncAttentionReader>(Attention)
            .AddSingleton<ISyncAttentionCloser>(Attention)
            .AddSingleton<IRemoteIndexReader>(Remote)
            .AddSingleton<IUploadRecorder>(Recorder)
            .AddSingleton<IDiskSpaceProbe>(Disk)
            .AddSingleton<ITrashRetentionSettings>(TrashRetention)
            .AddKeyedSingleton<IRemoteFileDownloader>(oneDrive, Downloader)
            .AddKeyedSingleton<IRemoteFileUploader>(oneDrive, Uploader)
            .AddKeyedSingleton<IRemoteFolderCreator>(oneDrive, Folders)
            .AddKeyedSingleton<IRemoteItemRemover>(oneDrive, Remover)
            .AddKeyedSingleton<IRemoteNamePolicy>(oneDrive, Names)
            .BuildServiceProvider();
    }

    public AccountId Account { get; }

    public MockFileSystem FileSystem { get; }

    public FakeTimeProvider Time { get; } = new(Start);

    public FixedSyncRootReader Roots { get; } = new();

    public InMemorySyncBaseline Baseline { get; } = new();

    public RecordingAttentionWriter Attention { get; } = new();

    public ScriptedRemoteIndex Remote { get; } = new();

    public ScriptedDownloader Downloader { get; } = new();

    public FakeUploader Uploader { get; } = new();

    public FakeFolderCreator Folders { get; } = new();

    public FakeItemRemover Remover { get; } = new();

    public FakeRemoteNamePolicy Names { get; } = new();

    public FakeUploadRecorder Recorder { get; }

    public FakeDiskSpaceProbe Disk { get; } = new();

    public FakeTrashRetentionSettings TrashRetention { get; } = new();

    public RecordingLoggerProvider Logs { get; } = new();

    public IConflictResolver Resolver => provider.GetRequiredService<IConflictResolver>();

    public IHeldDeletionDecider Decider => provider.GetRequiredService<IHeldDeletionDecider>();

    public IAccountSyncLock SyncLock => provider.GetRequiredService<IAccountSyncLock>();

    public static async Task<ResolutionFixture> CreateAsync(CancellationToken cancellationToken)
    {
        var accounts = new InMemoryAccountStore();
        var added = (AccountAddOutcome.Added)await accounts.AddAsync(AccountFixtures.Registration("00000000-0000-0000-a1b2-c3d4e5f60718.9188040d-6c67-4c5b-b112-36a304b66dad"), cancellationToken);

        return new ResolutionFixture(added.AccountId, accounts);
    }

    public static UploadPrecondition UploadMustMatch(string cTag) => UploadPreconditionFactory.CreateMustMatch(RemoteIds.CTag(cTag)).Match(precondition => precondition, _ => throw new InvalidOperationException("The test cTag must be valid."));

    public static RemovePrecondition RemoveMustMatchContent(string cTag) => RemovePreconditionFactory.CreateMustMatchContent(RemoteIds.CTag(cTag)).Match(precondition => precondition, _ => throw new InvalidOperationException("The test cTag must be valid."));

    public static RelativeSyncPath Path(string value) => SyncPaths.Of(value);

    public static string FullPath(string relative) => $"{Root}/{relative}";

    public IFileInfo Local(string relative, string content)
    {
        var full = FullPath(relative);
        _ = FileSystem.Directory.CreateDirectory(FileSystem.Path.GetDirectoryName(full)!);
        FileSystem.File.WriteAllText(full, content);
        FileSystem.File.SetLastWriteTimeUtc(full, Start.AddMinutes(-10).UtcDateTime);

        return FileSystem.FileInfo.New(full);
    }

    public SyncBaselineEntry Synced(string relative, string content, string itemId, string cTag = "c1")
    {
        var info = Local(relative, content);
        var entry = SyncBaselineEntryFactory.Create(Account, Path(relative), RemoteIds.Of(itemId), SyncBaselineKindFactory.CreateFile(info.Length, new DateTimeOffset(info.LastWriteTimeUtc, TimeSpan.Zero), RemoteIds.Hash("old"), RemoteIds.CTag(cTag), RemoteIds.ETag("e1")));
        Baseline.Add(entry);

        return entry;
    }

    public SyncBaselineEntry SyncedFolder(string relative, string itemId)
    {
        _ = FileSystem.Directory.CreateDirectory(FullPath(relative));
        var entry = SyncBaselineEntryFactory.Create(Account, Path(relative), RemoteIds.Of(itemId), SyncBaselineKindFactory.CreateFolder());
        Baseline.Add(entry);

        return entry;
    }

    public ConflictId Conflict(string relative, ConflictKind kind, SideSnapshot? local = null, SideSnapshot? remote = null, string? seenCTag = null)
    {
        var id = new ConflictId(Guid.NewGuid());
        Attention.Seed(SyncAttentionItemFactory.CreateConflict(id, Account, Path(relative), kind, local ?? ConflictFindings.Present(), remote ?? (seenCTag is null ? ConflictFindings.Present() : SideSnapshotFactory.CreatePresent(10, Start, Option.None<ContentHash>(), Option.Some(RemoteIds.CTag(seenCTag)))), Start));

        return id;
    }

    public HeldDeletionId Hold(DeletionDirection direction, params string[] paths)
    {
        var id = new HeldDeletionId(Guid.NewGuid());
        Attention.Seed(SyncAttentionItemFactory.CreateHeldDeletions(id, Account, SelectionTestRules.PairAt("Docs"), direction, paths.Select(Path), Start));

        return id;
    }

    public IReadOnlyList<string> TrashedFiles() =>
        FileSystem.Directory.Exists(FullPath(".sync-trash")) ? [.. FileSystem.Directory.EnumerateFiles(FullPath(".sync-trash"), "*", SearchOption.AllDirectories)] : [];

    public string FilesUnder(string relative) => string.Join(",", FileSystem.Directory.EnumerateFiles(FullPath(relative)).Select(FileSystem.Path.GetFileName).OfType<string>().Order(StringComparer.Ordinal));

    public void Dispose()
    {
        provider.Dispose();
        Logs.Dispose();
    }
}
