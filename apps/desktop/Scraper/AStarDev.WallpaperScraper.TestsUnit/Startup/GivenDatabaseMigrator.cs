using AStar.Dev.FunctionalParadigm;
using AStarDev.ControlDb;
using AStarDev.WallpaperScraper.Startup;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace AStarDev.WallpaperScraper.TestsUnit.Startup;

public sealed class GivenDatabaseMigrator : IDisposable
{
    private readonly SqliteConnection connection = new("Data Source=:memory:");
    private bool disposed;

    [Fact]
    public async Task when_migration_succeeds_then_the_result_is_a_success()
    {
        connection.Open();
        var dbContextFactory = new TestDbContextFactory(CreateOptionsIgnoringPendingModelChanges(connection));

        var result = await DatabaseMigrator.MigrateAsync(dbContextFactory, NullLogger.Instance);

        result.ShouldBeOfType<Success<UnitFp>>();
    }

    [Fact]
    public async Task when_the_db_context_factory_throws_then_the_result_is_a_failure()
    {
        var dbContextFactory = Substitute.For<IDbContextFactory<ControlDbContext>>();
        dbContextFactory.CreateDbContextAsync(Arg.Any<CancellationToken>())
            .Returns(Task.FromException<ControlDbContext>(new InvalidOperationException("Simulated connection failure")));

        var result = await DatabaseMigrator.MigrateAsync(dbContextFactory, NullLogger.Instance);

        result.ShouldBeOfType<Failure<UnitFp>>();
    }

    [Fact]
    public async Task when_the_db_context_factory_throws_then_the_failure_carries_the_original_exception()
    {
        var expectedException = new InvalidOperationException("Simulated connection failure");
        var dbContextFactory = Substitute.For<IDbContextFactory<ControlDbContext>>();
        dbContextFactory.CreateDbContextAsync(Arg.Any<CancellationToken>()).Returns(Task.FromException<ControlDbContext>(expectedException));

        var result = await DatabaseMigrator.MigrateAsync(dbContextFactory, NullLogger.Instance);

        ((Failure<UnitFp>)result).Exception.ShouldBe(expectedException);
    }

    [Fact]
    public async Task when_the_db_context_factory_throws_then_the_error_tap_logs_the_failure()
    {
        var dbContextFactory = Substitute.For<IDbContextFactory<ControlDbContext>>();
        dbContextFactory.CreateDbContextAsync(Arg.Any<CancellationToken>())
            .Returns(Task.FromException<ControlDbContext>(new InvalidOperationException("Simulated connection failure")));
        var logger = new RecordingLogger();

        await DatabaseMigrator.MigrateAsync(dbContextFactory, logger);

        logger.ErrorLogCount.ShouldBe(1);
    }

    [Fact]
    public async Task when_migration_succeeds_then_the_error_tap_does_not_log_anything()
    {
        connection.Open();
        var dbContextFactory = new TestDbContextFactory(CreateOptionsIgnoringPendingModelChanges(connection));
        var logger = new RecordingLogger();

        await DatabaseMigrator.MigrateAsync(dbContextFactory, logger);

        logger.ErrorLogCount.ShouldBe(0);
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    private static DbContextOptions<ControlDbContext> CreateOptionsIgnoringPendingModelChanges(SqliteConnection sqliteConnection) =>
        new DbContextOptionsBuilder<ControlDbContext>()
            .UseSqlite(sqliteConnection)
            .ConfigureWarnings(warnings => warnings.Ignore(RelationalEventId.PendingModelChangesWarning))
            .Options;

    private void Dispose(bool disposing)
    {
        if (disposed)
            return;

        disposed = true;

        if (disposing)
            connection.Dispose();
    }

    private sealed class TestDbContextFactory(DbContextOptions<ControlDbContext> options) : IDbContextFactory<ControlDbContext>
    {
        public ControlDbContext CreateDbContext() => new(options);

        public Task<ControlDbContext> CreateDbContextAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult(new ControlDbContext(options));
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
