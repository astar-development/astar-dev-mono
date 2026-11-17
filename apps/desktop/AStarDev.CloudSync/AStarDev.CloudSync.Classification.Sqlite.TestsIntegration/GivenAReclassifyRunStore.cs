using AStarDev.FunctionalParadigm;
using static AStarDev.CloudSync.Classification.Sqlite.TestsIntegration.ClassificationData;

namespace AStarDev.CloudSync.Classification.Sqlite.TestsIntegration;

public sealed class GivenAReclassifyRunStore : MigratedDatabaseTests
{
    private static readonly DateTimeOffset FinishedAt = new(2026, 11, 17, 9, 30, 15, TimeSpan.FromHours(1));

    private static ReclassifyRunRecord Run(string engineVersion, string? seedVersion) => ReclassifyRunRecordFactory.Create(engineVersion, seedVersion is null ? Option.None<string>() : Option.Some(seedVersion), FinishedAt);

    private async Task<Option<ReclassifyRunRecord>> LastAsync() => Ok(await Database.Runs.GetLastCompletedAsync(CancellationToken));

    [Fact]
    public async Task when_no_run_has_completed_then_none_is_read()
    {
        (await LastAsync()).ShouldBe(Option.None<ReclassifyRunRecord>());
    }

    [Fact]
    public async Task when_a_run_is_recorded_then_its_versions_and_finish_time_are_read_back()
    {
        var run = Run("1", "seed-v1");

        _ = Ok(await Database.Runs.SetLastCompletedAsync(run, CancellationToken));

        (await LastAsync()).ShouldBe(Option.Some(run));
    }

    [Fact]
    public async Task when_a_run_without_a_seed_is_recorded_then_it_is_read_back_without_a_seed()
    {
        var run = Run("1", null);

        _ = Ok(await Database.Runs.SetLastCompletedAsync(run, CancellationToken));

        (await LastAsync()).ShouldBe(Option.Some(run));
    }

    [Fact]
    public async Task when_a_second_run_is_recorded_then_it_replaces_the_first()
    {
        _ = Ok(await Database.Runs.SetLastCompletedAsync(Run("1", "seed-v1"), CancellationToken));

        _ = Ok(await Database.Runs.SetLastCompletedAsync(Run("2", "seed-v2"), CancellationToken));

        (await LastAsync()).ShouldBe(Option.Some(Run("2", "seed-v2")));
    }

    [Fact]
    public async Task when_runs_are_recorded_by_twenty_parallel_calls_then_one_of_them_is_stored()
    {
        _ = await InParallelAsync(20, async index => Ok(await Database.Runs.SetLastCompletedAsync(Run($"{index}", "seed-v1"), CancellationToken)));

        (await LastAsync()).Match(run => int.Parse(run.EngineVersion, System.Globalization.CultureInfo.InvariantCulture), () => -1).ShouldBeInRange(0, 19);
    }

    [Fact]
    public async Task when_the_service_restarts_then_the_run_is_still_there()
    {
        _ = Ok(await Database.Runs.SetLastCompletedAsync(Run("1", "seed-v1"), CancellationToken));

        await Database.RestartAsync();

        (await LastAsync()).ShouldBe(Option.Some(Run("1", "seed-v1")));
    }
}
