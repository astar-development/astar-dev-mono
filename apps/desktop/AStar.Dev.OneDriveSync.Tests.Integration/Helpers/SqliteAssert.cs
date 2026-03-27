using Microsoft.Data.Sqlite;

namespace AStar.Dev.OneDriveSync.Tests.Integration.Helpers;

/// <summary>
///     Assertion helpers for raw SQLite queries against test-only stub tables.
/// </summary>
internal static class SqliteAssert
{
    /// <summary>
    ///     Asserts that <paramref name="tableName" /> contains exactly
    ///     <paramref name="expected" /> rows whose <c>account_id</c> column matches
    ///     <paramref name="accountId" />.
    /// </summary>
    public static void ChildRowCount(SqliteConnection connection, string tableName, Guid accountId, long expected)
    {
        using var cmd = connection.CreateCommand();
        cmd.CommandText = $"SELECT COUNT(*) FROM {tableName} WHERE account_id = '{accountId.ToString("D").ToUpperInvariant()}'";
        var actual = (long?)cmd.ExecuteScalar();
        actual.ShouldBe(expected);
    }
}
