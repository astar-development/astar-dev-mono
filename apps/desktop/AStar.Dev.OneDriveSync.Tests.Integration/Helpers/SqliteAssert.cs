using Microsoft.Data.Sqlite;

namespace AStar.Dev.OneDriveSync.Tests.Integration.Helpers;

internal static class SqliteAssert
{
    public static void ChildRowCount(SqliteConnection connection, string tableName, Guid accountId, long expected)
    {
        using SqliteCommand cmd = connection.CreateCommand();
        cmd.CommandText = $"SELECT COUNT(*) FROM {tableName} WHERE account_id = $accountId";
        _ = cmd.Parameters.AddWithValue("$accountId", accountId.ToString("D").ToUpperInvariant());
        var actual = (long?)cmd.ExecuteScalar();

        actual.ShouldBe(expected);
    }
}
