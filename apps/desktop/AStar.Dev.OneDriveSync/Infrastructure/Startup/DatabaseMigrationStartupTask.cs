using AStar.Dev.OneDriveSync.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace AStar.Dev.OneDriveSync.Infrastructure.Startup;

public sealed class DatabaseMigrationStartupTask(IServiceProvider services) : IStartupTask
{
    public const string TaskName = "DatabaseMigration";

    string IStartupTask.Name => TaskName;

    public async Task RunAsync(CancellationToken ct)
    {
        // AppDbContext is scoped — create a scope so it is disposed correctly after migration
        await using AsyncServiceScope scope     = services.CreateAsyncScope();
        AppDbContext dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        await dbContext.Database.MigrateAsync(ct).ConfigureAwait(false);
    }
}
