using Microsoft.Data.Sqlite;

namespace AStar.Dev.Database.Compare.Tests.Unit;

public class GivenASqliteDatabaseWithNamedRows : IDisposable
{
    private readonly List<(string, bool)> expectedNames;

    public GivenASqliteDatabaseWithNamedRows() => expectedNames = [("Action", false), ("Cosplay", true)];

    readonly string databasePath = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid()}.db");

    [Fact]
    public void when_reading_names_then_returns_every_value_from_the_column()
    {
        string connectionString = $"Data Source={databasePath}";

        using (var connection = new SqliteConnection(connectionString))
        {
            connection.Open();
            using var command = connection.CreateCommand();
            command.CommandText = "CREATE TABLE FileClassification (Id INTEGER PRIMARY KEY, Name TEXT NOT NULL, IncludeInSearch BOOLEAN); INSERT INTO FileClassification (Name, IncludeInSearch) VALUES ('Action', 0), ('Cosplay', 1);";
            command.ExecuteNonQuery();
        }

        var nameReader = new SqliteNameReader();

        var names = nameReader.ReadNames(connectionString, "FileClassification", "Name, IncludeInSearch");

        names.ShouldBe(expectedNames);
    }

    public void Dispose()
    {
        SqliteConnection.ClearAllPools();
        File.Delete(databasePath);
        GC.SuppressFinalize(this);
    }
}
