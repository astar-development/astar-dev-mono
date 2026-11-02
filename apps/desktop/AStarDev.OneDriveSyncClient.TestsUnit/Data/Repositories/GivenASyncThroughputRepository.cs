using AStar.Dev.Infrastructure.AppDb;
using AStarDev.OneDriveSyncClient.Data.Repositories;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace AStarDev.OneDriveSyncClient.TestsUnit.Data.Repositories;

public sealed class GivenASyncThroughputRepository
{
    private static readonly DateTimeOffset Now = new(2026, 11, 2, 12, 0, 0, TimeSpan.Zero);

    private static (IDbContextFactory<AppDbContext> factory, SqliteConnection connection) CreateSqliteFactory()
    {
        var connection = new SqliteConnection("DataSource=:memory:");
        connection.Open();
        using (var cmd = connection.CreateCommand())
        {
            cmd.CommandText = "PRAGMA foreign_keys = OFF;";
            _ = cmd.ExecuteNonQuery();
        }

        var options = new DbContextOptionsBuilder<AppDbContext>().UseSqlite(connection).Options;
        using (var seedingContext = new AppDbContext(options))
        {
            _ = seedingContext.Database.EnsureCreated();
        }

        var factory = Substitute.For<IDbContextFactory<AppDbContext>>();
        factory.CreateDbContextAsync(Arg.Any<CancellationToken>()).Returns(_ => Task.FromResult(new AppDbContext(options)));

        return (factory, connection);
    }

    [Fact]
    public async Task when_a_sample_is_recorded_then_it_is_returned_by_get_since()
    {
        var (factory, connection) = CreateSqliteFactory();
        await using var connectionScope = connection;
        var sut = new SyncThroughputRepository(factory);

        await sut.RecordAsync(new AccountId("acc-1"), 1234, Now, TestContext.Current.CancellationToken);
        var result = await sut.GetSinceAsync(Now.AddHours(-2), TestContext.Current.CancellationToken);

        result.Count.ShouldBe(1);
        result[0].Bytes.ShouldBe(1234);
        result[0].AccountId.ShouldBe(new AccountId("acc-1"));
        result[0].CompletedAt.ShouldBe(Now);
    }

    [Fact]
    public async Task when_get_since_is_called_then_older_samples_are_excluded()
    {
        var (factory, connection) = CreateSqliteFactory();
        await using var connectionScope = connection;
        var sut = new SyncThroughputRepository(factory);
        await sut.RecordAsync(new AccountId("acc-1"), 1, Now.AddHours(-3), TestContext.Current.CancellationToken);
        await sut.RecordAsync(new AccountId("acc-1"), 2, Now.AddMinutes(-30), TestContext.Current.CancellationToken);

        var result = await sut.GetSinceAsync(Now.AddHours(-2), TestContext.Current.CancellationToken);

        result.Select(sample => sample.Bytes).ShouldBe([2L]);
    }

    [Fact]
    public async Task when_delete_older_than_is_called_then_only_newer_samples_remain()
    {
        var (factory, connection) = CreateSqliteFactory();
        await using var connectionScope = connection;
        var sut = new SyncThroughputRepository(factory);
        await sut.RecordAsync(new AccountId("acc-1"), 1, Now.AddHours(-30), TestContext.Current.CancellationToken);
        await sut.RecordAsync(new AccountId("acc-1"), 2, Now.AddHours(-1), TestContext.Current.CancellationToken);

        await sut.DeleteOlderThanAsync(Now.AddHours(-24), TestContext.Current.CancellationToken);
        var result = await sut.GetSinceAsync(Now.AddHours(-48), TestContext.Current.CancellationToken);

        result.Select(sample => sample.Bytes).ShouldBe([2L]);
    }
}
