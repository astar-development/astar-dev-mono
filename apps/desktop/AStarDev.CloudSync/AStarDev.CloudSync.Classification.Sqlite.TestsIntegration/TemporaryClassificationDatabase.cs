using System.IO.Abstractions;
using AStarDev.CloudSync.Accounts;
using AStarDev.CloudSync.Accounts.Sqlite;
using AStarDev.CloudSync.Sync;
using AStarDev.CloudSync.Sync.Sqlite;
using AStarDev.FunctionalParadigm;
using AStarDev.Utilities;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.DependencyInjection;
using Testably.Abstractions;
using Testably.Abstractions.Testing;

namespace AStarDev.CloudSync.Classification.Sqlite.TestsIntegration;

public sealed class TemporaryClassificationDatabase : IAsyncDisposable
{
    private const string Home = "/home/jbarden";

    private readonly Func<ServiceProvider> build;
    private readonly IDirectoryInfo testFolder;
    private ServiceProvider serviceProvider;

    private TemporaryClassificationDatabase(ServiceProvider serviceProvider, IDirectoryInfo testFolder, string databasePath, Func<ServiceProvider> build)
    {
        this.serviceProvider = serviceProvider;
        this.testFolder = testFolder;
        DatabasePath = databasePath;
        this.build = build;
    }

    public string DatabasePath { get; }

    public ITermStore Terms => serviceProvider.GetRequiredService<ITermStore>();

    public ICategoryStore Categories => serviceProvider.GetRequiredService<ICategoryStore>();

    public IFileClassificationStore Links => serviceProvider.GetRequiredService<IFileClassificationStore>();

    public ISeedStateStore SeedState => serviceProvider.GetRequiredService<ISeedStateStore>();

    public IClassifiableFileSource Files => serviceProvider.GetRequiredService<IClassifiableFileSource>();

    public ISyncBaselineWriter BaselineWriter => serviceProvider.GetRequiredService<ISyncBaselineWriter>();

    public IAccountStore AccountStore => serviceProvider.GetRequiredService<IAccountStore>();

    public ICloudSyncDatabase Database => serviceProvider.GetRequiredService<ICloudSyncDatabase>();

    public static async Task<TemporaryClassificationDatabase> CreateMigratedAsync(CancellationToken cancellationToken)
    {
        var testFolder = new RealFileSystem().Directory.CreateDirectory(Path.GetTempPath().CombinePath($"cloudsync-classification-tests-{Guid.NewGuid():N}"));
        var databaseFile = testFolder.FileSystem.FileInfo.New(testFolder.FullName.CombinePath("data").CombinePath("cloudsync.db"));
        var fileSystem = new MockFileSystem(options => options.SimulatingOperatingSystem(SimulationMode.Linux));
        _ = fileSystem.Directory.CreateDirectory(Home);
        var services = new ServiceCollection()
            .AddLogging()
            .AddSingleton<IFileSystem>(fileSystem)
            .AddSingleton<IHomeFolder>(new TestHomeFolder(fileSystem.DirectoryInfo.New(Home)))
            .AddCloudSyncPersistence(_ => databaseFile)
            .AddSyncPersistence(_ => databaseFile)
            .AddClassificationPersistence(_ => databaseFile);
        var build = () => services.BuildServiceProvider();
        var database = new TemporaryClassificationDatabase(build(), testFolder, databaseFile.FullName, build);
        var migrated = await database.Database.MigrateAsync(cancellationToken);
        if (migrated.Match(_ => true, _ => false))
            return database;

        await database.DisposeAsync();

        throw new InvalidOperationException("The test database could not be migrated.");
    }

    public async Task RestartAsync()
    {
        await serviceProvider.DisposeAsync();
        SqliteConnection.ClearAllPools();
        serviceProvider = build();
    }

    public async ValueTask DisposeAsync()
    {
        await serviceProvider.DisposeAsync();
        SqliteConnection.ClearAllPools();
        testFolder.Delete(recursive: true);
    }
}
