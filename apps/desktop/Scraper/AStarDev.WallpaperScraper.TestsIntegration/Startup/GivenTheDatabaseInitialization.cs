using AStarDev.ControlDb;
using AStarDev.WallpaperScraper.Startup;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Shouldly;
using Xunit;

namespace AStarDev.WallpaperScraper.TestsIntegration.Startup;

public sealed class GivenTheDatabaseInitialization : IDisposable
{
    private readonly string databasePath = Path.Combine(Path.GetTempPath(), $"astardev-WallpaperScraper-initialization-{Guid.CreateVersion7():N}.db");
    private readonly ServiceProvider serviceProvider;
    private bool disposed;

    public GivenTheDatabaseInitialization() =>
        serviceProvider = new ServiceCollection().AddDataServices(databasePath).BuildServiceProvider();

    [Fact]
    public async Task when_initialization_is_started_then_the_database_is_migrated_when_it_completes()
    {
        var factory = serviceProvider.GetRequiredService<IDbContextFactory<ControlDbContext>>();
        var initialization = new DatabaseInitialization(factory, NullLogger<DatabaseInitialization>.Instance);

        await initialization.ReadyAsync().WaitAsync(TimeSpan.FromSeconds(30), TestContext.Current.CancellationToken);

        await using var context = await factory.CreateDbContextAsync(TestContext.Current.CancellationToken);
        (await context.Database.GetPendingMigrationsAsync(TestContext.Current.CancellationToken)).ShouldBeEmpty();
    }

    [Fact]
    public void when_initialization_is_requested_more_than_once_then_the_same_task_is_returned_so_the_migration_runs_once()
    {
        var factory = serviceProvider.GetRequiredService<IDbContextFactory<ControlDbContext>>();
        var initialization = new DatabaseInitialization(factory, NullLogger<DatabaseInitialization>.Instance);

        var first = initialization.ReadyAsync();
        var second = initialization.ReadyAsync();

        second.ShouldBeSameAs(first);
    }

    [Fact]
    public async Task when_the_migration_fails_then_the_failure_is_surfaced_through_the_task_rather_than_thrown_on_request()
    {
        var initialization = new DatabaseInitialization(new FailingFactory(), NullLogger<DatabaseInitialization>.Instance);

        var ready = initialization.ReadyAsync();

        var exception = await Should.ThrowAsync<InvalidOperationException>(() => ready.WaitAsync(TimeSpan.FromSeconds(30), TestContext.Current.CancellationToken));
        exception.Message.ShouldBe("cannot open database");
    }

    public void Dispose()
    {
        if (disposed) return;

        disposed = true;
        serviceProvider.Dispose();
        if (File.Exists(databasePath)) File.Delete(databasePath);
    }

    private sealed class FailingFactory : IDbContextFactory<ControlDbContext>
    {
        public ControlDbContext CreateDbContext() => throw new InvalidOperationException("cannot open database");
    }
}
