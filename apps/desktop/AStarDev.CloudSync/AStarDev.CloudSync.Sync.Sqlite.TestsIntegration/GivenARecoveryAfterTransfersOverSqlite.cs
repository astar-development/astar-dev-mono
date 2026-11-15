using System.IO.Abstractions;
using AStarDev.CloudSync.Accounts;
using AStarDev.FunctionalParadigm;
using Microsoft.Extensions.DependencyInjection;
using static AStarDev.CloudSync.Sync.Sqlite.TestsIntegration.RemoteIndexData;

namespace AStarDev.CloudSync.Sync.Sqlite.TestsIntegration;

public sealed class GivenARecoveryAfterTransfersOverSqlite : IAsyncLifetime
{
    private const string Root = TemporarySyncDatabase.Home + "/CloudSync/OneDrive";

    private static readonly DateTime Written = new(2026, 11, 1, 10, 0, 0, DateTimeKind.Utc);

    private readonly ScriptedPasses passes = new();
    private readonly RecordingUploader uploader = new();
    private readonly RecordingDownloader downloader = new();

    private TemporarySyncDatabase? database;
    private AccountId account;
    private int pageNumber;

    private static CancellationToken CancellationToken => TestContext.Current.CancellationToken;

    private TemporarySyncDatabase Database => database ?? throw new InvalidOperationException("The database is created before each test.");

    public async ValueTask InitializeAsync()
    {
        var oneDrive = CloudProviderFactory.CreateOneDrive();
        database = await TemporarySyncDatabase.CreateMigratedAsync(CancellationToken, services => services
            .AddCloudSync()
            .AddSingleton<IRemoteIndexUpdater>(passes)
            .AddKeyedSingleton<IRemoteFileUploader>(oneDrive, uploader)
            .AddKeyedSingleton<IRemoteFileDownloader>(oneDrive, downloader)
            .AddKeyedSingleton<IRemoteFolderCreator>(oneDrive, new RefusingFolderCreator())
            .AddKeyedSingleton<IRemoteItemRemover>(oneDrive, new RefusingItemRemover())
            .AddKeyedSingleton<IRemoteNamePolicy>(oneDrive, new AcceptingNamePolicy()));
        var key = ProviderAccountKeyFactory.Create("key-1").Match(created => created, _ => throw new InvalidOperationException("The test key must be valid."));
        var registration = AccountRegistrationFactory.Create(key, oneDrive, "Personal", "personal@example.com", new DriveId("8A3F2C1D0E9B7A65"), StorageUsageFactory.Create(1, 2));
        account = (await Database.AccountStore.AddAsync(registration, CancellationToken)).ShouldBeOfType<AccountAddOutcome.Added>().AccountId;
        _ = Database.FileSystem.Directory.CreateDirectory(Root + "/Docs");
        _ = await Database.Writer.SetAsync(account, Database.Folder(Root), CancellationToken);
        _ = await Database.RuleWriter.ApplyAsync(account, [SelectionChangeFactory.CreateIncludeFolder(SyncPathsOf("Docs"), Option.Some(Id("docs")), SyncDirectionFactory.CreateTwoWay())], CancellationToken);
        await ApplyAsync(Folder("docs", "root-id", "Docs"));
    }

    public async ValueTask DisposeAsync()
    {
        if (database is not null)
            await database.DisposeAsync();
    }

    private static LocalRelativePath SyncPathsOf(string path) => LocalRelativePathFactory.Create(path).Match(created => created, _ => throw new InvalidOperationException("The test path must be valid."));

    private Task<Result<RemoteIndexDelta, ErrorResponse>> ApplyAsync(params RemoteItemSnapshot[] items) => Database.IndexWriter.ApplyPageAsync(account, Caught($"c{pageNumber++}", items), CancellationToken);

    private IFileInfo WriteLocalFile(string name)
    {
        var path = $"{Root}/Docs/{name}";
        Database.FileSystem.File.WriteAllText(path, RemoteContent.Text);
        Database.FileSystem.File.SetLastWriteTimeUtc(path, Written);

        return Database.FileSystem.FileInfo.New(path);
    }

    private async Task<SyncBaselineEntry> WriteLocalFileWithBaselineAsync(string name, string itemId)
    {
        var info = WriteLocalFile(name);
        var hash = (await new Sha256ContentHasher().HashAsync(info, CancellationToken)).Match(value => value, _ => throw new InvalidOperationException("The test file must hash."));

        return SyncBaselineEntryFactory.Create(account, SyncPaths.Of($"Docs/{name}"), Id(itemId), SyncBaselineKindFactory.CreateFile(info.Length, new DateTimeOffset(Written, TimeSpan.Zero), hash, Tag("c-1"), ETag($"e-{itemId}")));
    }

    private async Task FinishedUploadBeforeTheCrashAsync(string name, string itemId)
    {
        var baseline = await WriteLocalFileWithBaselineAsync(name, itemId);
        _ = await Database.UploadRecorder.RecordAsync(account, baseline, File(itemId, "docs", name, "c-1", eTag: $"e-{itemId}"), CancellationToken);
    }

    private async Task FinishedDownloadBeforeTheCrashAsync(string name, string itemId)
    {
        var baseline = await WriteLocalFileWithBaselineAsync(name, itemId);
        _ = await ApplyAsync(File(itemId, "docs", name, "c-1", eTag: $"e-{itemId}"));
        _ = await Database.BaselineWriter.UpsertAsync(account, [baseline], CancellationToken);
    }

    private async Task<IReadOnlyList<RecoveryOutcome>> RecoverAsync()
    {
        _ = await Database.Journal.StartAsync(account, SyncTriggerFactory.CreateScheduled(), CancellationToken);

        return await Database.Services.GetRequiredService<IInterruptedSyncRecovery>().RecoverAsync(CancellationToken);
    }

    [Fact]
    public async Task when_a_file_finished_uploading_before_the_crash_then_recovery_uploads_only_the_files_that_are_left()
    {
        await FinishedUploadBeforeTheCrashAsync("a.txt", "item-a");
        WriteLocalFile("b.txt");

        var outcomes = await RecoverAsync();

        outcomes.ShouldHaveSingleItem().ShouldBeOfType<RecoveryOutcome.Resumed>();
        uploader.Uploaded.ShouldBe(["b.txt"]);
    }

    [Fact]
    public async Task when_a_file_finished_downloading_before_the_crash_then_recovery_downloads_only_the_files_that_are_left()
    {
        await FinishedDownloadBeforeTheCrashAsync("a.txt", "item-a");
        _ = await ApplyAsync(File("item-b", "docs", "b.txt", "c-1"));

        var outcomes = await RecoverAsync();

        outcomes.ShouldHaveSingleItem().ShouldBeOfType<RecoveryOutcome.Resumed>();
        downloader.Downloaded.ShouldBe(["item-b"]);
    }

    [Fact]
    public async Task when_recovery_runs_twice_then_nothing_that_finished_is_transferred_again()
    {
        await FinishedUploadBeforeTheCrashAsync("a.txt", "item-a");
        WriteLocalFile("b.txt");
        _ = await RecoverAsync();

        _ = await RecoverAsync();

        uploader.Uploaded.ShouldBe(["b.txt"]);
    }
}
