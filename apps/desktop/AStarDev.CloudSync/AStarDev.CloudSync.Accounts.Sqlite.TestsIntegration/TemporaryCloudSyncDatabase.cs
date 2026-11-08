using System.IO.Abstractions;
using AStarDev.CloudSync.Accounts;
using AStarDev.CloudSync.Accounts.Sqlite;
using AStarDev.CloudSync.Core.Persistence;
using AStarDev.CloudSync.TestHelpers.Common;
using AStarDev.FunctionalParadigm;
using AStarDev.Utilities;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Testably.Abstractions;

namespace AStarDev.CloudSync.Accounts.Sqlite.TestsIntegration;

public sealed class TemporaryCloudSyncDatabase : IAsyncDisposable
{
    private const string CloudSyncCategoryPrefix = "AStarDev.CloudSync.";

    private readonly ServiceProvider serviceProvider;
    private readonly IDirectoryInfo testFolder;

    private TemporaryCloudSyncDatabase(ServiceProvider serviceProvider, IDirectoryInfo testFolder)
    {
        this.serviceProvider = serviceProvider;
        this.testFolder = testFolder;
    }

    public IAccountStore AccountStore => serviceProvider.GetRequiredService<IAccountStore>();

    public ICloudSyncDatabase Database => serviceProvider.GetRequiredService<ICloudSyncDatabase>();

    public RecordingLogger<RecordingLoggerProvider> Logger => serviceProvider.GetRequiredService<RecordingLoggerProvider>().Logger;

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
        var loggerProvider = new RecordingLoggerProvider();
        var serviceProvider = new ServiceCollection()
            .AddSingleton(loggerProvider)
            .AddLogging(logging => logging.ClearProviders().AddProvider(loggerProvider).AddFilter((category, _) => category?.StartsWith(CloudSyncCategoryPrefix, StringComparison.Ordinal) == true))
            .AddCloudSyncPersistence(_ => databaseFile)
            .BuildServiceProvider();

        return new TemporaryCloudSyncDatabase(serviceProvider, testFolder);
    }
}
