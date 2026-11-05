using AStarDev.CloudSync.Core.Accounts;
using AStarDev.CloudSync.Infrastructure.Persistence;
using AStarDev.CloudSync.TestHelpers.Common;
using AStarDev.Utilities;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Testably.Abstractions;

namespace AStarDev.CloudSync.Infrastructure.TestsIntegration.Persistence;

public sealed class TemporaryCloudSyncDatabase : IAsyncDisposable
{
    private readonly ServiceProvider serviceProvider;
    private readonly string databaseFolder;

    private TemporaryCloudSyncDatabase(ServiceProvider serviceProvider, string databaseFolder)
    {
        this.serviceProvider = serviceProvider;
        this.databaseFolder = databaseFolder;
    }

    public IAccountStore AccountStore => serviceProvider.GetRequiredService<IAccountStore>();

    public RecordingLogger<SqliteAccountStore> Logger => serviceProvider.GetRequiredService<RecordingLogger<SqliteAccountStore>>();

    public static async Task<TemporaryCloudSyncDatabase> CreateAsync(bool migrated, CancellationToken cancellationToken)
    {
        var fileSystem = new RealFileSystem();
        var databaseFolder = Path.GetTempPath().CombinePath($"cloudsync-tests-{Guid.NewGuid():N}");
        fileSystem.Directory.CreateDirectory(databaseFolder);
        var logger = new RecordingLogger<SqliteAccountStore>();
        var serviceProvider = new ServiceCollection()
            .AddSingleton(logger)
            .AddSingleton<ILogger<SqliteAccountStore>>(logger)
            .AddCloudSyncPersistence(fileSystem.FileInfo.New(databaseFolder.CombinePath("cloudsync.db")))
            .BuildServiceProvider();

        if (migrated)
        {
            await using var dbContext = await serviceProvider.GetRequiredService<IDbContextFactory<CloudSyncDbContext>>().CreateDbContextAsync(cancellationToken);
            await dbContext.Database.MigrateAsync(cancellationToken);
        }

        return new TemporaryCloudSyncDatabase(serviceProvider, databaseFolder);
    }

    public async ValueTask DisposeAsync()
    {
        await serviceProvider.DisposeAsync();
        SqliteConnection.ClearAllPools();
        Directory.Delete(databaseFolder, recursive: true);
    }
}
