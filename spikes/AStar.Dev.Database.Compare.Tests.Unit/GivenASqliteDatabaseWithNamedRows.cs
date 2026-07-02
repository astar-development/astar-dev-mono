using Microsoft.Data.Sqlite;

namespace AStar.Dev.Database.Compare.Tests.Unit;

public class GivenASqliteDatabaseWithNamedRows : IDisposable
{
    static readonly string[] expectedNames = ["Action", "Cosplay"];

    readonly string databasePath = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid()}.db");

    [Fact]
    public void when_reading_names_then_returns_every_value_from_the_column()
    {
        var connectionString = $"Data Source={databasePath}";

        using (var connection = new SqliteConnection(connectionString))
        {
            connection.Open();
            using var command = connection.CreateCommand();
            command.CommandText = "CREATE TABLE FileClassification (Id INTEGER PRIMARY KEY, Name TEXT NOT NULL); INSERT INTO FileClassification (Name) VALUES ('Action'), ('Cosplay');";
            command.ExecuteNonQuery();
        }

        var nameReader = new SqliteNameReader();

        var names = nameReader.ReadNames(connectionString, "FileClassification", "Name");

        names.ShouldBe(expectedNames);
    }

    public void Dispose()
    {
        SqliteConnection.ClearAllPools();
        File.Delete(databasePath);
        GC.SuppressFinalize(this);
    }
}
