using Microsoft.Data.Sqlite;

namespace AStar.Dev.Database.Compare;

public sealed class SqliteNameReader : INameReader
{
    public IReadOnlyList<(string, bool)> ReadNames(string connectionString, string tableName, string columnNames)
    {
        using var connection = new SqliteConnection(connectionString);
        connection.Open();

        using var command = connection.CreateCommand();
        command.CommandText = $"SELECT {columnNames} FROM {tableName}";

        using var reader = command.ExecuteReader();
        var names = new List<(string, bool)>();

        while (reader.Read())
        {
            names.Add((reader.GetString(0), reader.GetBoolean(1)));
        }

        return names;
    }
}
