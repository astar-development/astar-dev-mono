using System.Data.Common;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace AStarDev.EFCoreSqlite;

/// <summary>Sets the SQLite busy timeout on every connection EF Core opens, so a command waiting for a lock another connection holds is retried by SQLite within milliseconds of its release. Without it Microsoft.Data.Sqlite sets no busy handler and polls a busy lock every 150 ms, so a writer behind a loop of short write transactions can miss the gaps between them for seconds. The 30 seconds match the default command timeout. Add it with <c>options.AddInterceptors(SqliteBusyTimeoutInterceptor.Instance)</c>.</summary>
public sealed class SqliteBusyTimeoutInterceptor : DbConnectionInterceptor
{
    private const string SetBusyTimeout = "PRAGMA busy_timeout = 30000;";

    private SqliteBusyTimeoutInterceptor()
    {
    }

    /// <summary>The shared instance; the interceptor holds no state.</summary>
    public static SqliteBusyTimeoutInterceptor Instance { get; } = new();

    /// <inheritdoc />
    public override void ConnectionOpened(DbConnection connection, ConnectionEndEventData eventData)
    {
        if (connection is null)
            return;

        using var command = connection.CreateCommand();
        command.CommandText = SetBusyTimeout;
        _ = command.ExecuteNonQuery();
    }

    /// <inheritdoc />
    public override async Task ConnectionOpenedAsync(DbConnection connection, ConnectionEndEventData eventData, CancellationToken cancellationToken = default)
    {
        if (connection is null)
            return;

        var command = connection.CreateCommand();
        await using (command.ConfigureAwait(false))
        {
            command.CommandText = SetBusyTimeout;
            _ = await command.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
        }
    }
}
