using AStar.Dev.Infrastructure.AppDb;
using AStar.Dev.Wallpaper.Scraper.Startup;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace AStar.Dev.Wallpaper.Scraper.Tests.Unit.Startup;

public sealed class GivenDatabaseMigrator : IDisposable
{
    private readonly SqliteConnection connection = new("Data Source=:memory:");

    [Fact]
    public async Task when_migration_succeeds_then_pending_migrations_are_applied()
    {
        connection.Open();
        var options = new DbContextOptionsBuilder<AppDbContext>().UseSqlite(connection).Options;
        var dbContextFactory = new TestDbContextFactory(options);

        await DatabaseMigrator.MigrateAsync(dbContextFactory, NullLogger.Instance);

        using var context = dbContextFactory.CreateDbContext();
        await Should.NotThrowAsync(() => context.FileClassificationCategories.ToListAsync(TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task when_the_db_context_factory_throws_then_the_exception_is_swallowed()
    {
        var dbContextFactory = Substitute.For<IDbContextFactory<AppDbContext>>();
        dbContextFactory.CreateDbContextAsync(Arg.Any<CancellationToken>())
            .Returns(Task.FromException<AppDbContext>(new InvalidOperationException("Simulated connection failure")));

        await Should.NotThrowAsync(() => DatabaseMigrator.MigrateAsync(dbContextFactory, NullLogger.Instance));
    }

    [Fact]
    public async Task when_the_db_context_factory_throws_then_the_error_tap_logs_the_failure()
    {
        var dbContextFactory = Substitute.For<IDbContextFactory<AppDbContext>>();
        dbContextFactory.CreateDbContextAsync(Arg.Any<CancellationToken>())
            .Returns(Task.FromException<AppDbContext>(new InvalidOperationException("Simulated connection failure")));
        var logger = new RecordingLogger();

        await DatabaseMigrator.MigrateAsync(dbContextFactory, logger);

        logger.ErrorLogCount.ShouldBe(1);
    }

    [Fact]
    public async Task when_migration_succeeds_then_the_error_tap_does_not_log_anything()
    {
        connection.Open();
        var options = new DbContextOptionsBuilder<AppDbContext>().UseSqlite(connection).Options;
        var dbContextFactory = new TestDbContextFactory(options);
        var logger = new RecordingLogger();

        await DatabaseMigrator.MigrateAsync(dbContextFactory, logger);

        logger.ErrorLogCount.ShouldBe(0);
    }

    public void Dispose() =>
        connection.Dispose();

    private sealed class TestDbContextFactory(DbContextOptions<AppDbContext> options) : IDbContextFactory<AppDbContext>
    {
        public AppDbContext CreateDbContext() => new(options);

        public Task<AppDbContext> CreateDbContextAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult(new AppDbContext(options));
    }

    private sealed class RecordingLogger : ILogger
    {
        public int ErrorLogCount { get; private set; }

        public IDisposable BeginScope<TState>(TState state) where TState : notnull => NullScope.Instance;

        public bool IsEnabled(LogLevel logLevel) =>
            true;

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
        {
            if (logLevel == LogLevel.Error) ErrorLogCount++;
        }

        private sealed class NullScope : IDisposable
        {
            public static readonly NullScope Instance = new();

            public void Dispose() { }
        }
    }
}
