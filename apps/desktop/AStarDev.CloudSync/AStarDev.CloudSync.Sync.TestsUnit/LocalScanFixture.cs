using System.IO.Abstractions;
using AStarDev.CloudSync.Accounts;
using AStarDev.CloudSync.TestHelpers.Common;
using AStarDev.FunctionalParadigm;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Time.Testing;
using Testably.Abstractions.Testing;

namespace AStarDev.CloudSync.Sync.TestsUnit;

public sealed class LocalScanFixture : IDisposable
{
    public static readonly AccountId Account = AccountIdFactory.Create(Guid.Parse("11111111-1111-1111-1111-111111111111"));

    private static readonly DateTimeOffset Start = new(2026, 11, 13, 12, 0, 0, TimeSpan.Zero);

    private readonly ServiceProvider provider;

    public LocalScanFixture()
    {
        FileSystem = new MockFileSystem(options => options.SimulatingOperatingSystem(SimulationMode.Linux));
        Time = new FakeTimeProvider(Start);
        Roots.Root = Option.Some(FileSystem.Directory.CreateDirectory(RootPath));
        provider = new ServiceCollection()
            .AddLogging(logging => logging.ClearProviders().AddProvider(Logs))
            .AddSingleton<System.TimeProvider>(Time)
            .AddSingleton<ISyncRootReader>(Roots)
            .AddSingleton<ISelectionRuleReader>(Rules)
            .AddSingleton<ISyncBaselineReader>(Baseline)
            .AddSingleton<ISyncBaselineWriter>(Baseline)
            .AddCloudSync()
            .AddSingleton<IContentHasher>(Hasher)
            .BuildServiceProvider();
    }

    public const string RootPath = "/sync/root";

    public MockFileSystem FileSystem { get; }

    public FakeTimeProvider Time { get; }

    public FixedSyncRootReader Roots { get; } = new();

    public ScriptedRuleReader Rules { get; } = new();

    public InMemorySyncBaseline Baseline { get; } = new();

    public CountingContentHasher Hasher { get; } = new();

    public RecordingLoggerProvider Logs { get; } = new();

    public ILocalChangeDetector Detector => provider.GetRequiredService<ILocalChangeDetector>();

    public void Include(params string[] folders) => Rules.Listing = Result.Success<IReadOnlyList<SelectionRule>, ErrorResponse>([.. folders.Select(folder => SelectionTestRules.Include(folder))]);

    public void SetRules(params SelectionRule[] rules) => Rules.Listing = Result.Success<IReadOnlyList<SelectionRule>, ErrorResponse>(rules);

    public IFileInfo Write(string relative, string content, TimeSpan? age = null)
    {
        var full = $"{RootPath}/{relative}";
        _ = FileSystem.Directory.CreateDirectory(FileSystem.Path.GetDirectoryName(full)!);
        FileSystem.File.WriteAllText(full, content);
        FileSystem.File.SetLastWriteTimeUtc(full, (Start - (age ?? TimeSpan.FromMinutes(10))).UtcDateTime);

        return FileSystem.FileInfo.New(full);
    }

    public void Folder(string relative) => FileSystem.Directory.CreateDirectory($"{RootPath}/{relative}");

    public SyncBaselineEntry Synced(string relative, string content, string itemId, TimeSpan? age = null)
    {
        var info = Write(relative, content, age);
        var hash = new Sha256ContentHasher().HashAsync(info, CancellationToken.None).GetAwaiter().GetResult().Match(value => value, _ => throw new InvalidOperationException("The test file must hash."));
        var entry = SyncBaselineEntryFactory.Create(Account, Path(relative), RemoteIds.Of(itemId), SyncBaselineKindFactory.CreateFile(info.Length, new DateTimeOffset(info.LastWriteTimeUtc, TimeSpan.Zero), hash, RemoteIds.CTag("c"), RemoteIds.ETag("e")));
        Baseline.Add(entry);

        return entry;
    }

    public SyncBaselineEntry SyncedFolder(string relative, string itemId)
    {
        Folder(relative);
        var entry = SyncBaselineEntryFactory.Create(Account, Path(relative), RemoteIds.Of(itemId), SyncBaselineKindFactory.CreateFolder());
        Baseline.Add(entry);

        return entry;
    }

    public static RelativeSyncPath Path(string value) => RelativeSyncPathFactory.Create(value).Match(path => path, _ => throw new InvalidOperationException("The test path must be valid."));

    public async Task<LocalChangeSet> ScanAsync(CancellationToken cancellationToken)
    {
        var result = await Detector.DetectAsync(Account, cancellationToken);

        return result.Match(set => set, failure => throw new InvalidOperationException($"The scan failed: {failure.GetType().Name}."));
    }

    public static string Describe(LocalChangeSet set) => string.Join(';', set.Changes.Select(change => change switch
    {
        LocalChange.Added added => $"Added:{added.Path.Value}:{added.Kind.GetType().Name}",
        LocalChange.Modified modified => $"Modified:{modified.Path.Value}",
        LocalChange.Deleted deleted => $"Deleted:{deleted.Path.Value}:{deleted.ItemId.Value}",
        LocalChange.Deferred deferred => $"Deferred:{deferred.Path.Value}",
        LocalChange.Unreadable unreadable => $"Unreadable:{unreadable.Path.Value}",
        _ => "?",
    }));

    public void Dispose() => provider.Dispose();
}
