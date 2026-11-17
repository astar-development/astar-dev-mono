using AStarDev.FunctionalParadigm;

namespace AStarDev.CloudSync.Classification.TestsUnit;

public sealed class GivenAReclassifyFreshnessCheck
{
    private static readonly DateTimeOffset FinishedAt = new(2026, 11, 17, 9, 0, 0, TimeSpan.Zero);

    private static CancellationToken CancellationToken => TestContext.Current.CancellationToken;

    private static ReclassifyRunRecord Run(string engineVersion, string? seedVersion) => ReclassifyRunRecordFactory.Create(engineVersion, seedVersion is null ? Option.None<string>() : Option.Some(seedVersion), FinishedAt);

    private static async Task<object> CheckAsync(InMemoryReclassifyRunStore runs, SeedingStateStore seedState) => await new ReclassifyFreshnessCheck(runs, seedState).CheckAsync(CancellationToken).MatchAsync(freshness => (object)freshness, failure => failure);

    [Fact]
    public async Task when_no_run_has_completed_then_it_is_stale()
    {
        var freshness = await CheckAsync(new InMemoryReclassifyRunStore(), new SeedingStateStore { Version = "v1" });

        freshness.ShouldBe(ReclassifyFreshnessFactory.CreateStale());
    }

    [Fact]
    public async Task when_the_last_run_used_the_current_engine_and_seed_then_it_is_up_to_date()
    {
        var runs = new InMemoryReclassifyRunStore { Last = Option.Some(Run(ClassificationEngineVersion.Current, "v1")) };

        var freshness = await CheckAsync(runs, new SeedingStateStore { Version = "v1" });

        freshness.ShouldBe(ReclassifyFreshnessFactory.CreateUpToDate());
    }

    [Fact]
    public async Task when_the_last_run_used_another_engine_version_then_it_is_stale()
    {
        var runs = new InMemoryReclassifyRunStore { Last = Option.Some(Run(ClassificationEngineVersion.Current + "-old", "v1")) };

        var freshness = await CheckAsync(runs, new SeedingStateStore { Version = "v1" });

        freshness.ShouldBe(ReclassifyFreshnessFactory.CreateStale());
    }

    [Fact]
    public async Task when_the_last_run_used_another_seed_version_then_it_is_stale()
    {
        var runs = new InMemoryReclassifyRunStore { Last = Option.Some(Run(ClassificationEngineVersion.Current, "v1")) };

        var freshness = await CheckAsync(runs, new SeedingStateStore { Version = "v2" });

        freshness.ShouldBe(ReclassifyFreshnessFactory.CreateStale());
    }

    [Fact]
    public async Task when_the_seed_version_differs_only_by_case_then_it_is_stale()
    {
        var runs = new InMemoryReclassifyRunStore { Last = Option.Some(Run(ClassificationEngineVersion.Current, "v1")) };

        var freshness = await CheckAsync(runs, new SeedingStateStore { Version = "V1" });

        freshness.ShouldBe(ReclassifyFreshnessFactory.CreateStale());
    }

    [Fact]
    public async Task when_the_last_run_had_no_seed_and_a_seed_is_applied_now_then_it_is_stale()
    {
        var runs = new InMemoryReclassifyRunStore { Last = Option.Some(Run(ClassificationEngineVersion.Current, null)) };

        var freshness = await CheckAsync(runs, new SeedingStateStore { Version = "v1" });

        freshness.ShouldBe(ReclassifyFreshnessFactory.CreateStale());
    }

    [Fact]
    public async Task when_neither_the_last_run_nor_now_has_a_seed_then_it_is_up_to_date()
    {
        var runs = new InMemoryReclassifyRunStore { Last = Option.Some(Run(ClassificationEngineVersion.Current, null)) };

        var freshness = await CheckAsync(runs, new SeedingStateStore());

        freshness.ShouldBe(ReclassifyFreshnessFactory.CreateUpToDate());
    }

    [Fact]
    public async Task when_the_run_record_cannot_be_read_then_the_failure_is_returned()
    {
        var freshness = await CheckAsync(new InMemoryReclassifyRunStore { FailReads = true }, new SeedingStateStore { Version = "v1" });

        freshness.ShouldBeOfType<ClassificationFailure.Unavailable>();
    }

    [Fact]
    public async Task when_the_seed_version_cannot_be_read_then_the_failure_is_returned()
    {
        var runs = new InMemoryReclassifyRunStore { Last = Option.Some(Run(ClassificationEngineVersion.Current, "v1")) };

        var freshness = await CheckAsync(runs, new SeedingStateStore { FailReads = true });

        freshness.ShouldBeOfType<ClassificationFailure.Unavailable>();
    }
}
