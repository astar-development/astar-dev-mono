using System.IO.Abstractions;
using AStarDev.CloudSync.Accounts;
using AStarDev.CloudSync.Accounts.Sqlite;
using AStarDev.CloudSync.Sync;
using AStarDev.FunctionalParadigm;
using AStarDev.Utilities;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.DependencyInjection;
using Testably.Abstractions;
using Testably.Abstractions.Testing;

namespace AStarDev.CloudSync.Sync.Sqlite.TestsIntegration;

public sealed class TemporarySyncDatabase : IAsyncDisposable
{
    public const string Home = "/home/jbarden";

    private readonly ServiceProvider serviceProvider;
    private readonly IDirectoryInfo testFolder;

    private TemporarySyncDatabase(ServiceProvider serviceProvider, IDirectoryInfo testFolder, MockFileSystem fileSystem)
    {
        this.serviceProvider = serviceProvider;
        this.testFolder = testFolder;
        FileSystem = fileSystem;
    }

    public MockFileSystem FileSystem { get; }

    public ISyncRootReader Reader => serviceProvider.GetRequiredService<ISyncRootReader>();

    public ISyncRootWriter Writer => serviceProvider.GetRequiredService<ISyncRootWriter>();

    public IAccountRemovalParticipant Participant => serviceProvider.GetServices<IAccountRemovalParticipant>().First(participant => participant.GetType().Name == "SqliteSyncRootStore");

    public IAccountRemovalParticipant RulesParticipant => serviceProvider.GetServices<IAccountRemovalParticipant>().First(participant => participant.GetType().Name == "SqliteSelectionRuleStore");

    public ISelectionRuleReader Rules => serviceProvider.GetRequiredService<ISelectionRuleReader>();

    public ISelectionRuleWriter RuleWriter => serviceProvider.GetRequiredService<ISelectionRuleWriter>();

    public ICloudSyncDatabase Database => serviceProvider.GetRequiredService<ICloudSyncDatabase>();

    public IAccountStore AccountStore => serviceProvider.GetRequiredService<IAccountStore>();

    public static TemporarySyncDatabase CreateUnmigrated()
    {
        var testFolder = new RealFileSystem().Directory.CreateDirectory(Path.GetTempPath().CombinePath($"cloudsync-sync-tests-{Guid.NewGuid():N}"));
        var databaseFile = testFolder.FileSystem.FileInfo.New(testFolder.FullName.CombinePath("data").CombinePath("cloudsync.db"));
        var fileSystem = new MockFileSystem(options => options.SimulatingOperatingSystem(SimulationMode.Linux));
        _ = fileSystem.Directory.CreateDirectory(Home);
        var serviceProvider = new ServiceCollection()
            .AddLogging()
            .AddSingleton<IFileSystem>(fileSystem)
            .AddSingleton<IHomeFolder>(new TestHomeFolder(fileSystem.DirectoryInfo.New(Home)))
            .AddCloudSyncPersistence(_ => databaseFile)
            .AddSyncPersistence(_ => databaseFile)
            .BuildServiceProvider();

        return new TemporarySyncDatabase(serviceProvider, testFolder, fileSystem);
    }

    public static async Task<TemporarySyncDatabase> CreateMigratedAsync(CancellationToken cancellationToken)
    {
        var database = CreateUnmigrated();
        var migrated = await database.Database.MigrateAsync(cancellationToken);

        if (migrated.Match(_ => true, _ => false))
            return database;

        await database.DisposeAsync();

        throw new InvalidOperationException("The test database could not be migrated.");
    }

    public IDirectoryInfo Folder(string path) => FileSystem.DirectoryInfo.New(path);

    public async ValueTask DisposeAsync()
    {
        await serviceProvider.DisposeAsync();
        SqliteConnection.ClearAllPools();
        testFolder.Delete(recursive: true);
    }
}
