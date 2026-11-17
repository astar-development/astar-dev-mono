using AStarDev.FunctionalParadigm;
using Microsoft.Data.Sqlite;

namespace AStarDev.CloudSync.Classification.Sqlite.TestsIntegration;

public sealed class GivenAClassificationMigration : MigratedDatabaseTests
{
    private static readonly string[] ExpectedTables = ["Accounts", "SyncBaseline", "Categories", "Terms", "FileLinks", "ClassificationState", "ReclassifyRun", "__ClassificationMigrationsHistory", "__SyncMigrationsHistory"];

    private async Task<List<string>> TablesAsync()
    {
        await using var connection = new SqliteConnection(new SqliteConnectionStringBuilder { DataSource = Database.DatabasePath, Pooling = false }.ToString());
        await connection.OpenAsync(CancellationToken);
        await using var command = connection.CreateCommand();
        command.CommandText = "SELECT name FROM sqlite_master WHERE type = 'table' ORDER BY name";
        var tables = new List<string>();
        await using var reader = await command.ExecuteReaderAsync(CancellationToken);
        while (await reader.ReadAsync(CancellationToken))
            tables.Add(reader.GetString(0));

        return tables;
    }

    [Fact]
    public async Task when_the_database_is_migrated_then_the_classification_tables_sit_beside_the_accounts_and_sync_tables()
    {
        var tables = await TablesAsync();

        ExpectedTables.ShouldBeSubsetOf(tables);
    }

    [Fact]
    public async Task when_the_database_is_migrated_twice_then_the_second_run_succeeds()
    {
        var second = await Database.Database.MigrateAsync(CancellationToken);

        second.Match(_ => true, _ => false).ShouldBeTrue();
    }
}
