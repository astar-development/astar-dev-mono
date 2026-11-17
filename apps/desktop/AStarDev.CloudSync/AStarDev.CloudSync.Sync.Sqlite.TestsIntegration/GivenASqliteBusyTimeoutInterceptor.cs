using AStarDev.EFCoreSqlite;
using Microsoft.Data.Sqlite;

namespace AStarDev.CloudSync.Sync.Sqlite.TestsIntegration;

public sealed class GivenASqliteBusyTimeoutInterceptor
{
    private static CancellationToken CancellationToken => TestContext.Current.CancellationToken;

    private static long BusyTimeout(SqliteConnection connection)
    {
        using var command = connection.CreateCommand();
        command.CommandText = "PRAGMA busy_timeout;";

        return (long)command.ExecuteScalar()!;
    }

    [Fact]
    public void when_a_connection_is_opened_synchronously_then_its_busy_timeout_is_five_seconds()
    {
        using var connection = new SqliteConnection("Data Source=:memory:");
        connection.Open();

        SqliteBusyTimeoutInterceptor.Instance.ConnectionOpened(connection, null!);

        BusyTimeout(connection).ShouldBe(5000L);
    }

    [Fact]
    public async Task when_a_connection_is_opened_asynchronously_then_its_busy_timeout_is_five_seconds()
    {
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync(CancellationToken);

        await SqliteBusyTimeoutInterceptor.Instance.ConnectionOpenedAsync(connection, null!, CancellationToken);

        BusyTimeout(connection).ShouldBe(5000L);
    }

    [Fact]
    public async Task when_no_connection_is_given_then_nothing_is_set_and_nothing_throws()
    {
        SqliteBusyTimeoutInterceptor.Instance.ConnectionOpened(null!, null!);

        await Should.NotThrowAsync(() => SqliteBusyTimeoutInterceptor.Instance.ConnectionOpenedAsync(null!, null!, CancellationToken));
    }
}
