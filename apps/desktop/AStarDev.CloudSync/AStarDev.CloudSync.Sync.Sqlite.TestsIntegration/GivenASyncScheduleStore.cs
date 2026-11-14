using AStarDev.FunctionalParadigm;

namespace AStarDev.CloudSync.Sync.Sqlite.TestsIntegration;

public sealed class GivenASyncScheduleStore : IAsyncLifetime
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

    private async Task<object> GetAsync() => (await Database.ScheduleSettings.GetAsync(CancellationToken)).Match(schedule => (object)schedule, error => error);

    private async Task<object> SetAsync(SyncSchedule schedule) => (await Database.ScheduleSettings.SetAsync(schedule, CancellationToken)).Match(saved => (object)saved, error => error);

    [Fact]
    public async Task when_nothing_was_saved_then_the_schedule_is_every_hour()
    {
        (await GetAsync()).ShouldBe(SyncScheduleFactory.CreateDefault());
    }

    [Fact]
    public async Task when_an_interval_is_saved_then_it_is_returned_and_read_back()
    {
        var saved = await SetAsync(SyncScheduleFactory.CreateEvery(TimeSpan.FromMinutes(30)));

        saved.ShouldBe(SyncScheduleFactory.CreateEvery(TimeSpan.FromMinutes(30)));
        (await GetAsync()).ShouldBe(SyncScheduleFactory.CreateEvery(TimeSpan.FromMinutes(30)));
    }

    [Fact]
    public async Task when_manual_only_is_saved_then_it_is_read_back_as_manual_only_not_as_the_default()
    {
        _ = await SetAsync(SyncScheduleFactory.CreateManualOnly());

        (await GetAsync()).ShouldBe(SyncScheduleFactory.CreateManualOnly());
    }

    [Fact]
    public async Task when_the_schedule_is_changed_several_times_then_the_last_one_wins()
    {
        _ = await SetAsync(SyncScheduleFactory.CreateEvery(TimeSpan.FromMinutes(15)));
        _ = await SetAsync(SyncScheduleFactory.CreateManualOnly());
        _ = await SetAsync(SyncScheduleFactory.CreateEvery(TimeSpan.FromHours(4)));

        (await GetAsync()).ShouldBe(SyncScheduleFactory.CreateEvery(TimeSpan.FromHours(4)));
    }

    [Fact]
    public async Task when_twenty_saves_race_then_none_fail_and_one_of_the_values_remains()
    {
        var choices = SyncScheduleFactory.Intervals.Select(SyncScheduleFactory.CreateEvery).ToList();
        var racing = Enumerable.Range(0, 20).Select(index => Task.Run(() => SetAsync(choices[index % choices.Count]), CancellationToken));

        var outcomes = await Task.WhenAll(racing);

        outcomes.ShouldAllBe(outcome => outcome is SyncSchedule);
        choices.ShouldContain((SyncSchedule)await GetAsync());
    }

    [Fact]
    public async Task when_the_database_is_unreadable_then_the_read_and_the_save_fail_without_throwing()
    {
        await using var broken = TemporarySyncDatabase.CreateUnmigrated();

        var read = await broken.ScheduleSettings.GetAsync(CancellationToken);
        var saved = await broken.ScheduleSettings.SetAsync(SyncScheduleFactory.CreateManualOnly(), CancellationToken);

        read.Match(_ => false, _ => true).ShouldBeTrue();
        saved.Match(_ => false, _ => true).ShouldBeTrue();
    }

    [Fact]
    public void when_the_module_is_added_then_the_schedule_settings_are_one_shared_instance()
    {
        Database.ScheduleSettings.ShouldBeSameAs(Database.ScheduleSettings);
    }
}
