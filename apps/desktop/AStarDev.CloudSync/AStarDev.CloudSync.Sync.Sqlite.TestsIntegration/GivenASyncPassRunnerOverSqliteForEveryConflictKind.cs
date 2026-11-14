using AStarDev.CloudSync.Accounts;
using AStarDev.FunctionalParadigm;
using Microsoft.Extensions.DependencyInjection;

namespace AStarDev.CloudSync.Sync.Sqlite.TestsIntegration;

public sealed class GivenASyncPassRunnerOverSqliteForEveryConflictKind : IAsyncLifetime
{
    private static readonly AccountId Account = AccountIdFactory.Create(Guid.Parse("11111111-1111-1111-1111-111111111111"));

    private readonly ScriptedPasses passes = new();

    private TemporarySyncDatabase? database;

    private static CancellationToken CancellationToken => TestContext.Current.CancellationToken;

    private TemporarySyncDatabase Database => database ?? throw new InvalidOperationException("The database is created before each test.");

    private static ConflictFinding Finding(string path, ConflictKind kind) => ConflictFindingFactory.Create(SyncPaths.Of(path), kind, SideSnapshotFactory.CreateAbsent(), SideSnapshotFactory.CreateAbsent());

    public async ValueTask InitializeAsync() =>
        database = await TemporarySyncDatabase.CreateMigratedAsync(CancellationToken, services => services
            .AddCloudSync()
            .AddSingleton<IRemoteIndexUpdater>(passes)
            .AddSingleton<ILocalChangeDetector>(passes)
            .AddSingleton<IDownloadPass>(passes)
            .AddSingleton<IUploadPass>(passes));

    public async ValueTask DisposeAsync()
    {
        if (database is not null)
            await database.DisposeAsync();
    }

    [Fact]
    public async Task when_the_download_and_upload_passes_report_a_conflict_of_every_kind_then_each_is_stored_in_sqlite_with_its_kind()
    {
        ConflictKind[] downloadKinds = [ConflictKindFactory.CreateBothModified(), ConflictKindFactory.CreateLocalModifiedRemoteDeleted(), ConflictKindFactory.CreateMoveTargetTaken()];
        ConflictKind[] uploadKinds = [ConflictKindFactory.CreateLocalDeletedRemoteModified(), ConflictKindFactory.CreateBothAdded(), ConflictKindFactory.CreateRemoteChangedDuringTransfer()];
        passes.Downloaded = [.. downloadKinds.Select((kind, index) => DownloadItemOutcomeFactory.CreateSentToConflicts(Finding($"Docs/d{index}.txt", kind)))];
        passes.Uploaded = [.. uploadKinds.Select((kind, index) => UploadItemOutcomeFactory.CreateSentToConflicts(Finding($"Docs/u{index}.txt", kind)))];

        _ = await Database.Services.GetRequiredService<ISyncPassRunner>().RunAsync(Account, SyncTriggerFactory.CreateManual(), CancellationToken);

        var stored = (await Database.Services.GetRequiredService<ISyncAttentionReader>().GetOpenAsync(Account, CancellationToken)).Match(items => items, _ => throw new InvalidOperationException("The attention queue could not be read."));
        stored.OfType<SyncAttentionItem.Conflict>().OrderBy(item => item.Path.Value).Select(item => item.Kind).ShouldBe([.. downloadKinds, .. uploadKinds]);
    }
}
