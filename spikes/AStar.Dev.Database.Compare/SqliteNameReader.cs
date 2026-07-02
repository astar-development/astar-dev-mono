using Microsoft.Data.Sqlite;

namespace AStar.Dev.Database.Compare;

public sealed class SqliteNameReader : INameReader
{
    public IReadOnlyList<string> ReadNames(string connectionString, string tableName, string columnName)
    {
        using var connection = new SqliteConnection(connectionString);
        connection.Open();

        using var command = connection.CreateCommand();
        command.CommandText = $"SELECT {columnName} FROM {tableName}";

        using var reader = command.ExecuteReader();
        var names = new List<string>();

        while (reader.Read())
        {
            names.Add(reader.GetString(0));
        }

        return names;
    }
}
