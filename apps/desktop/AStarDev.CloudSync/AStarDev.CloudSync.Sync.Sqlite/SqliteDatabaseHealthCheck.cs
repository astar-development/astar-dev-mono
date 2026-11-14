using System.IO.Abstractions;
using AStarDev.FunctionalParadigm;
using AStarDev.LoggingExtensions;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Logging;

namespace AStarDev.CloudSync.Sync.Sqlite;

/// <inheritdoc />
/// <param name="databaseFile">The CloudSync database file.</param>
/// <param name="logger">Reports a damaged or unreadable database, with the exception type only.</param>
internal sealed class SqliteDatabaseHealthCheck(IFileInfo databaseFile, ILogger<SqliteDatabaseHealthCheck> logger) : IDatabaseHealthCheck
{
    private const string Sound = "ok";

    /// <inheritdoc />
    public Task<Result<Unit, DatabaseHealthFailure>> CheckAsync(CancellationToken cancellationToken) =>
        databaseFile.Exists
            ? Try.RunAsync(() => QuickCheckAsync(cancellationToken), cancellationToken).MatchAsync(Verdict, Unreadable)
            : Task.FromResult(Result.Success<Unit, DatabaseHealthFailure>(Unit.Instance));

    private Result<Unit, DatabaseHealthFailure> Verdict(IReadOnlyList<string> rows)
    {
        if (rows is [Sound])
            return Result.Success<Unit, DatabaseHealthFailure>(Unit.Instance);

        LogMessage.Error(logger, "The database integrity check found damage.", "QuickCheckReportedDamage");

        return Result.Failure<Unit, DatabaseHealthFailure>(DatabaseHealthFailureFactory.CreateDamaged());
    }

    private Result<Unit, DatabaseHealthFailure> Unreadable(Exception exception)
    {
        LogMessage.Error(logger, "The database could not be read for the integrity check.", exception.ErrorType());

        return Result.Failure<Unit, DatabaseHealthFailure>(DatabaseHealthFailureFactory.CreateUnreadable());
    }

    private async Task<IReadOnlyList<string>> QuickCheckAsync(CancellationToken cancellationToken)
    {
        var connectionString = new SqliteConnectionStringBuilder { DataSource = databaseFile.FullName, Mode = SqliteOpenMode.ReadOnly, Pooling = false }.ToString();
        await using var connection = new SqliteConnection(connectionString);
        await connection.OpenAsync(cancellationToken).ConfigureAwait(false);
        await using var command = connection.CreateCommand();
        command.CommandText = "PRAGMA quick_check";
        await using var reader = await command.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false);
        var rows = new List<string>();
        while (await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
            rows.Add(reader.GetString(0));

        return rows;
    }
}
