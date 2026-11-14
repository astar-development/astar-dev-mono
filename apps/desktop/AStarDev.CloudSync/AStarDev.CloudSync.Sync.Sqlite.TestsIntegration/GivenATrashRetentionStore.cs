using AStarDev.FunctionalParadigm;
using Microsoft.Data.Sqlite;

namespace AStarDev.CloudSync.Sync.Sqlite.TestsIntegration;

public sealed class GivenATrashRetentionStore : IAsyncLifetime
{
    private TemporarySyncDatabase? database;

    private static CancellationToken CancellationToken => TestContext.Current.CancellationToken;

    private TemporarySyncDatabase Database => database ?? throw new InvalidOperationException("The database is created before each test.");

    public async ValueTask InitializeAsync() => database = await TemporarySyncDatabase.CreateMigratedAsync(CancellationToken);

    public async ValueTask DisposeAsync()
    {
        if (database is not null)
            await database.DisposeAsync();
    }

    private async Task<object> GetAsync() => (await Database.TrashRetentionSettings.GetAsync(CancellationToken)).Match(options => (object)options, error => error);

    private async Task<object> SetAsync(int days) => (await Database.TrashRetentionSettings.SetAsync(LocalTrashOptionsFactory.CreateFromDays(days), CancellationToken)).Match(saved => (object)saved, error => error);

    private async Task StoreRawAsync(int days)
    {
        await using var connection = new SqliteConnection(new SqliteConnectionStringBuilder { DataSource = Database.DatabasePath }.ToString());
        await connection.OpenAsync(CancellationToken);
        await using var command = connection.CreateCommand();
        command.CommandText = $"INSERT OR REPLACE INTO TrashRetention (Id, Days) VALUES (1, {days})";
        _ = await command.ExecuteNonQueryAsync(CancellationToken);
    }

    [Fact]
    public async Task when_nothing_was_saved_then_the_retention_is_thirty_days()
    {
        (await GetAsync()).ShouldBe(LocalTrashOptionsFactory.Create());
    }

    [Fact]
    public async Task when_a_retention_is_saved_then_it_is_returned_and_read_back()
    {
        var saved = await SetAsync(90);

        saved.ShouldBe(LocalTrashOptionsFactory.CreateFromDays(90));
        (await GetAsync()).ShouldBe(LocalTrashOptionsFactory.CreateFromDays(90));
    }

    [Fact]
    public async Task when_the_retention_is_changed_several_times_then_the_last_one_wins()
    {
        _ = await SetAsync(7);
        _ = await SetAsync(365);
        _ = await SetAsync(14);

        (await GetAsync()).ShouldBe(LocalTrashOptionsFactory.CreateFromDays(14));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-3)]
    [InlineData(366)]
    [InlineData(1000000)]
    public async Task when_an_invalid_value_is_stored_then_the_default_is_read_back(int days)
    {
        await StoreRawAsync(days);

        (await GetAsync()).ShouldBe(LocalTrashOptionsFactory.Create());
    }

    [Fact]
    public async Task when_twenty_saves_race_then_none_fail_and_one_of_the_values_remains()
    {
        var racing = Enumerable.Range(0, 20).Select(index => Task.Run(() => SetAsync(LocalTrashOptionsFactory.Choices[index % LocalTrashOptionsFactory.Choices.Count]), CancellationToken));

        var outcomes = await Task.WhenAll(racing);

        outcomes.ShouldAllBe(outcome => outcome is LocalTrashOptions);
        LocalTrashOptionsFactory.Choices.Select(LocalTrashOptionsFactory.CreateFromDays).ShouldContain((LocalTrashOptions)await GetAsync());
    }

    [Fact]
    public async Task when_the_database_is_unreadable_then_the_read_and_the_save_fail_without_throwing()
    {
        await using var broken = TemporarySyncDatabase.CreateUnmigrated();

        var read = await broken.TrashRetentionSettings.GetAsync(CancellationToken);
        var saved = await broken.TrashRetentionSettings.SetAsync(LocalTrashOptionsFactory.Create(), CancellationToken);

        read.Match(_ => false, _ => true).ShouldBeTrue();
        saved.Match(_ => false, _ => true).ShouldBeTrue();
    }

    [Fact]
    public void when_the_module_is_added_then_the_trash_retention_settings_are_one_shared_instance()
    {
        Database.TrashRetentionSettings.ShouldBeSameAs(Database.TrashRetentionSettings);
    }
}
