using System.IO.Abstractions;
using AStarDev.CloudSync.Accounts;
using AStarDev.CloudSync.Core.Persistence;
using AStarDev.CloudSync.Infrastructure.Persistence;
using AStarDev.CloudSync.TestHelpers.Common;
using AStarDev.FunctionalParadigm;
using AStarDev.Utilities;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Testably.Abstractions;

namespace AStarDev.CloudSync.Infrastructure.TestsIntegration.Persistence;

public sealed class TemporaryCloudSyncDatabase : IAsyncDisposable
{
    private readonly ServiceProvider serviceProvider;
    private readonly IDirectoryInfo testFolder;

    private TemporaryCloudSyncDatabase(ServiceProvider serviceProvider, IDirectoryInfo testFolder)
    {
        this.serviceProvider = serviceProvider;
        this.testFolder = testFolder;
    }

    public IAccountStore AccountStore => serviceProvider.GetRequiredService<IAccountStore>();

    public ICloudSyncDatabase Database => serviceProvider.GetRequiredService<ICloudSyncDatabase>();

    public RecordingLogger<SqliteAccountStore> StoreLogger => serviceProvider.GetRequiredService<RecordingLogger<SqliteAccountStore>>();

    public RecordingLogger<SqliteCloudSyncDatabase> DatabaseLogger => serviceProvider.GetRequiredService<RecordingLogger<SqliteCloudSyncDatabase>>();

    public static TemporaryCloudSyncDatabase CreateUnmigrated() => Create(testFolder => testFolder.FileSystem.FileInfo.New(testFolder.FullName.CombinePath("data").CombinePath("cloudsync.db")));

    public static TemporaryCloudSyncDatabase CreateAtAFolder() => Create(testFolder => testFolder.FileSystem.FileInfo.New(testFolder.CreateSubdirectory("cloudsync.db").FullName));

    public static async Task<TemporaryCloudSyncDatabase> CreateMigratedAsync(CancellationToken cancellationToken)
    {
        var database = CreateUnmigrated();
        var migrated = await Try.RunAsync(() => database.Database.MigrateAsync(cancellationToken), cancellationToken)
            .MatchAsync(result => result.Match(_ => true, _ => false), _ => false);

        if (migrated)
            return database;

        await database.DisposeAsync();

        throw new InvalidOperationException("The test database could not be migrated.");
    }

    public async ValueTask DisposeAsync()
    {
        await serviceProvider.DisposeAsync();
        SqliteConnection.ClearAllPools();
        testFolder.Delete(recursive: true);
    }

    private static TemporaryCloudSyncDatabase Create(Func<IDirectoryInfo, IFileInfo> databaseFileIn)
    {
        var testFolder = new RealFileSystem().Directory.CreateDirectory(Path.GetTempPath().CombinePath($"cloudsync-tests-{Guid.NewGuid():N}"));
        var databaseFile = databaseFileIn(testFolder);
        var storeLogger = new RecordingLogger<SqliteAccountStore>();
        var databaseLogger = new RecordingLogger<SqliteCloudSyncDatabase>();
        var serviceProvider = new ServiceCollection()
            .AddSingleton(storeLogger)
            .AddSingleton<ILogger<SqliteAccountStore>>(storeLogger)
            .AddSingleton(databaseLogger)
            .AddSingleton<ILogger<SqliteCloudSyncDatabase>>(databaseLogger)
            .AddCloudSyncPersistence(_ => databaseFile)
            .BuildServiceProvider();

        return new TemporaryCloudSyncDatabase(serviceProvider, testFolder);
    }
}
