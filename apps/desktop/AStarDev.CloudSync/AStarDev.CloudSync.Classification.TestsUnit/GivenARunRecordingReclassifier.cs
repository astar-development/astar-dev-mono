using AStarDev.CloudSync.TestHelpers.Classification;
using AStarDev.CloudSync.TestHelpers.Common;
using AStarDev.FunctionalParadigm;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Time.Testing;

namespace AStarDev.CloudSync.Classification.TestsUnit;

public sealed class GivenARunRecordingReclassifier
{
    private static readonly DateTimeOffset Now = new(2026, 11, 17, 9, 30, 0, TimeSpan.Zero);

    private static CancellationToken CancellationToken => TestContext.Current.CancellationToken;

    private readonly FakeClassificationReclassifier inner = new();
    private readonly SeedingStateStore seedState = new() { Version = "v1" };
    private readonly RecordingLogger<RunRecordingReclassifier> logger = new();

    private RunRecordingReclassifier Create(IReclassifyRunStore runs) => Create(runs, seedState);

    private RunRecordingReclassifier Create(IReclassifyRunStore runs, ISeedStateStore seeds) => new(inner, seeds, runs, ReclassifyOptionsFactory.Create(10, 1, TimeSpan.Zero, TimeSpan.Zero, new FakeTimeProvider(Now)), logger);

    private static ReclassifyCounts Counts() => ReclassifyCountsFactory.Create(3, 1, 0);

    public static TheoryData<string> UnfinishedOutcomes => ["cancelled", "failed", "already running"];

    private static ReclassifyOutcome Unfinished(string outcome) => outcome switch
    {
        "cancelled" => ReclassifyOutcomeFactory.CreateCancelled(Counts()),
        "failed" => ReclassifyOutcomeFactory.CreateFailed(Counts()),
        _ => ReclassifyOutcomeFactory.CreateAlreadyRunning()
    };

    [Fact]
    public async Task when_waiting_until_idle_then_the_inner_reclassifier_is_waited_on()
    {
        await Create(new InMemoryReclassifyRunStore()).WaitUntilIdleAsync(CancellationToken);

        inner.WaitCount.ShouldBe(1);
    }

    [Fact]
    public async Task when_a_run_completes_then_the_engine_version_the_seed_version_and_the_finish_time_are_recorded()
    {
        var runs = new InMemoryReclassifyRunStore();

        _ = await Create(runs).StartAsync(new Progress<ReclassifyProgress>(), CancellationToken);

        runs.Last.ShouldBe(Option.Some(ReclassifyRunRecordFactory.Create(ClassificationEngineVersion.Current, Option.Some("v1"), Now)));
    }

    [Fact]
    public async Task when_a_run_completes_then_the_inner_outcome_is_returned()
    {
        inner.Outcome = ReclassifyOutcomeFactory.CreateCompleted(Counts());

        var outcome = await Create(new InMemoryReclassifyRunStore()).StartAsync(new Progress<ReclassifyProgress>(), CancellationToken);

        outcome.ShouldBeSameAs(inner.Outcome);
    }

    [Theory]
    [MemberData(nameof(UnfinishedOutcomes))]
    public async Task when_a_run_does_not_complete_then_nothing_is_recorded(string unfinished)
    {
        inner.Outcome = Unfinished(unfinished);
        var runs = new InMemoryReclassifyRunStore();

        var outcome = await Create(runs).StartAsync(new Progress<ReclassifyProgress>(), CancellationToken);

        (outcome, runs.SetCalls).ShouldBe((inner.Outcome, 0));
    }

    [Fact]
    public async Task when_the_seed_changes_during_the_run_then_the_seed_version_from_the_start_is_recorded()
    {
        inner.Gate = new TaskCompletionSource();
        var runs = new InMemoryReclassifyRunStore();

        var running = Create(runs).StartAsync(new Progress<ReclassifyProgress>(), CancellationToken);
        await inner.Started.Task;
        seedState.Version = "v2";
        inner.Gate.SetResult();
        _ = await running;

        runs.Last.Match(run => run.SeedVersion, () => Option.None<string>()).ShouldBe(Option.Some("v1"));
    }

    [Fact]
    public async Task when_no_seed_is_applied_then_the_run_is_recorded_without_a_seed_version()
    {
        var runs = new InMemoryReclassifyRunStore();

        _ = await Create(runs, new SeedingStateStore()).StartAsync(new Progress<ReclassifyProgress>(), CancellationToken);

        runs.Last.Match(run => run.SeedVersion, () => Option.Some("not recorded")).ShouldBe(Option.None<string>());
    }

    [Fact]
    public async Task when_the_seed_version_cannot_be_read_then_the_run_is_recorded_without_a_seed_version()
    {
        var runs = new InMemoryReclassifyRunStore();

        _ = await Create(runs, new SeedingStateStore { FailReads = true }).StartAsync(new Progress<ReclassifyProgress>(), CancellationToken);

        runs.Last.Match(run => run.SeedVersion, () => Option.Some("not recorded")).ShouldBe(Option.None<string>());
    }

    [Fact]
    public async Task when_the_seed_read_is_cancelled_then_the_run_still_starts_and_nothing_throws()
    {
        inner.Outcome = ReclassifyOutcomeFactory.CreateCancelled(Counts());

        var outcome = await Create(new InMemoryReclassifyRunStore(), new CancellingSeedStateStore()).StartAsync(new Progress<ReclassifyProgress>(), CancellationToken);

        (outcome, inner.StartCount).ShouldBe((inner.Outcome, 1));
    }

    [Fact]
    public async Task when_the_record_cannot_be_written_then_the_run_still_completes_and_a_warning_is_logged()
    {
        inner.Outcome = ReclassifyOutcomeFactory.CreateCompleted(Counts());

        var outcome = await Create(new InMemoryReclassifyRunStore { FailWrites = true }).StartAsync(new Progress<ReclassifyProgress>(), CancellationToken);

        outcome.ShouldBeSameAs(inner.Outcome);
        logger.Entries.ShouldContain(entry => entry.Level == LogLevel.Warning);
    }

    [Fact]
    public async Task when_the_record_write_is_cancelled_then_the_completed_outcome_is_still_returned()
    {
        inner.Outcome = ReclassifyOutcomeFactory.CreateCompleted(Counts());

        var outcome = await Create(new CancellingReclassifyRunStore()).StartAsync(new Progress<ReclassifyProgress>(), CancellationToken);

        outcome.ShouldBeSameAs(inner.Outcome);
    }

    [Fact]
    public async Task when_the_caller_cancels_after_the_run_completed_then_the_run_is_still_recorded()
    {
        using var cancelled = new CancellationTokenSource();
        await cancelled.CancelAsync();
        var runs = new TokenHonouringReclassifyRunStore();

        _ = await Create(runs).StartAsync(new Progress<ReclassifyProgress>(), cancelled.Token);

        runs.Last.Match(run => run.EngineVersion, () => "not recorded").ShouldBe(ClassificationEngineVersion.Current);
    }
}
